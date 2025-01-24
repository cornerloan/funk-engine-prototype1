using Godot;
using System;
using System.Collections.Generic;
using FileAccess = Godot.FileAccess;

public partial class SpawnManager : Node2D
{
    private PackedScene _redNoteScene;
    private PackedScene _yellowNoteScene;
    private PackedScene _greenNoteScene;
    private PackedScene _blueNoteScene;

    private int _redXPosition = 385;
    private int _yellowXPosition = 513;
    private int _greenXPosition = 641;
    private int _blueXPosition = 769;
    private int _yPosition = -100;

    private float _songStartTime;
    private Dictionary<int, List<string>> _notesToSpawn = new Dictionary<int, List<string>>();
    
    private ProgressBar _progressBar;
    private int _songLength;

    [Export] private string _filePath = "res://songs/practice.chart";
    private string _fileContents;

    public override void _Ready()
    {
        //ensure song file exists
        if (FileAccess.FileExists(_filePath))
        {
            _fileContents = FileAccess.Open(_filePath, FileAccess.ModeFlags.Read).GetAsText();
            GD.Print("File loaded successfully.");
        }
        else
        {
            GD.PrintErr($"File {_filePath} not found.");
            return;
        }

        // Load note scenes
        _redNoteScene = GD.Load<PackedScene>("res://scenes/note scenes/redNoteScene.tscn");
        _yellowNoteScene = GD.Load<PackedScene>("res://scenes/note scenes/yellowNoteScene.tscn");
        _greenNoteScene = GD.Load<PackedScene>("res://scenes/note scenes/greenNoteScene.tscn");
        _blueNoteScene = GD.Load<PackedScene>("res://scenes/note scenes/blueNoteScene.tscn");

        // Fetch the progress bar node
        _progressBar = GetNode<ProgressBar>("../Control/ProgressBar");
        if (_progressBar == null)
        {
            GD.PrintErr("Progress bar could not be found.");
        }

        ParseChartFile();
        _songStartTime = Time.GetTicksMsec() / 1000f; // Start time in seconds
    }

    private void ParseChartFile()
    {
        var lines = new List<string>(_fileContents.Split('\n'));
        for (int i = 0; i < lines.Count; i++)
        {
            lines[i] = lines[i].Trim();
        }

        bool inExpertSingleSection = false;

        foreach (string line in lines)
        {
            if (line == "[ExpertSingle]")
            {
                inExpertSingleSection = true;
                continue;
            }
            if (line == "}" && inExpertSingleSection)
            {
                inExpertSingleSection = false;
                continue;
            }

            if (inExpertSingleSection)
            {
                var parts = line.Split('=');
                if (parts.Length == 2)
                {
                    int frame = int.Parse(parts[0].Trim());
                    _songLength = Math.Max(_songLength, frame);
                    var noteData = parts[1].Trim().Split(' ');
                    if (noteData.Length >= 3 && noteData[0] == "N")
                    {
                        int noteType = int.Parse(noteData[1]);
                        string noteColor = GetNoteColor(noteType);

                        if (!_notesToSpawn.ContainsKey(frame))
                        {
                            _notesToSpawn[frame] = new List<string>();
                        }
                        _notesToSpawn[frame].Add(noteColor);
                    }
                }
            }
        }
    }

    private string GetNoteColor(int noteType)
    {
        return noteType switch
        {
            0 => "Red",
            1 => "Yellow",
            2 => "Green",
            3 => "Blue",
            _ => null,
        };
    }

    public override void _Process(double delta)
    {
        float currentTime = Time.GetTicksMsec() / 1000f;
        float elapsedTime = currentTime - _songStartTime;

        int currentFrame = (int)(elapsedTime * 192);
        int bufferFrames = 1;

        var framesToSpawn = new List<int>(_notesToSpawn.Keys);
        foreach (var frame in framesToSpawn)
        {
            if (frame <= currentFrame + bufferFrames && frame >= currentFrame - bufferFrames)
            {
                foreach (string noteColor in _notesToSpawn[frame])
                {
                    SpawnNoteByColor(noteColor);
                }
                _notesToSpawn.Remove(frame);
            }
        }

        UpdateProgressBar(currentFrame);
    }

    private void SpawnNoteByColor(string color)
    {
        PackedScene noteScene = color switch
        {
            "Red" => _redNoteScene,
            "Yellow" => _yellowNoteScene,
            "Green" => _greenNoteScene,
            "Blue" => _blueNoteScene,
            _ => null,
        };

        int xPosition = color switch
        {
            "Red" => _redXPosition,
            "Yellow" => _yellowXPosition,
            "Green" => _greenXPosition,
            "Blue" => _blueXPosition,
            _ => 0,
        };

        if (noteScene != null)
        {
            SpawnNote(noteScene, xPosition, _yPosition, color);
        }
        else
        {
            GD.PrintErr($"Invalid note color: {color}");
        }
    }

    private void SpawnNote(PackedScene noteScene, int xPosition, int yPosition, string color)
    {
        var noteInstance = noteScene.Instantiate() as Node2D;
        if (noteInstance != null)
        {
            noteInstance.Position = new Vector2(xPosition, yPosition);
            AddChild(noteInstance);
        }
        else
        {
            GD.PrintErr($"Failed to instantiate note scene for color {color}.");
        }
    }

    private void UpdateProgressBar(int currentFrame)
    {
        if (_progressBar != null)
        {
            _progressBar.Value = ((float)currentFrame / (_songLength + 1600)) * 100f;
        }
        else
        {
            GD.PrintErr("Progress bar is null.");
        }
    }
}
