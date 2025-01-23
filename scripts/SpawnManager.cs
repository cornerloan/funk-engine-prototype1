using Godot;
using System;
using System.IO;
using System.Collections.Generic;

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

    private string _songPath = "songs/practice.chart";
    
    private ProgressBar _progressBar;
    private int _songLength;

    public override void _Ready()
    {
        // Load note scenes
        _redNoteScene = GD.Load<PackedScene>("res://scenes/note scenes/redNoteScene.tscn");
        _yellowNoteScene = GD.Load<PackedScene>("res://scenes/note scenes/yellowNoteScene.tscn");
        _greenNoteScene = GD.Load<PackedScene>("res://scenes/note scenes/greenNoteScene.tscn");
        _blueNoteScene = GD.Load<PackedScene>("res://scenes/note scenes/blueNoteScene.tscn");

        // parse song if it exists
        if (!File.Exists(_songPath))
        {
            GD.PrintErr("Song file not found: " + _songPath);
            return;
        }
        ParseChartFile();
        _songStartTime = Time.GetTicksMsec() / 1000f; // Get start time in seconds
        
        // Properly fetch the progress bar node
        _progressBar = GetNode<ProgressBar>("../Control/ProgressBar");
        if (_progressBar == null)
        {
            GD.PrintErr("Progress bar not found.");
        }
    }

    private void ParseChartFile()
    {
        string[] lines = File.ReadAllLines(_songPath);
        bool inExpertSingleSection = false;

        foreach (string line in lines)
        {
            // note data comes after "[ExpertSingle]"
            if (line.Trim() == "[ExpertSingle]")
            {
                inExpertSingleSection = true;
                continue;
            }
            if (line.Trim() == "}" && inExpertSingleSection)
            {
                inExpertSingleSection = false;
                continue;
            }

            // can now parse the note data
            if (inExpertSingleSection)
            {
                string[] parts = line.Split('=');
                if (parts.Length == 2)
                {
                    int frame = int.Parse(parts[0].Trim());
                    _songLength = frame;
                    string[] noteData = parts[1].Trim().Split(' ');
                    if (noteData.Length >= 3 && noteData[0] == "N")
                    {
                        int noteType = int.Parse(noteData[1]);
                        string noteColor = GetNoteColor(noteType);
                        
                        GD.Print($"Frame: {frame}, Note Type: {noteType}, Color: {noteColor}");

                        //each frame can contain multiple colors of notes, so each frame is a list
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
        float currentTime = Time.GetTicksMsec() / 1000f; // current time in seconds
        float elapsedTime = currentTime - _songStartTime;

        int currentFrame = (int)(elapsedTime * 192); // 192 frames per second
        int bufferFrames = 1; // frame buffer, sometimes notes dont spawn without it

        foreach (var frame in _notesToSpawn.Keys)
        {
            if (frame <= currentFrame + bufferFrames && frame >= currentFrame - bufferFrames)
            {
                // spawn the notes for this frame and delete the data from _notesToSpawn
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
        // scene names by color
        PackedScene noteScene = color switch
        {
            "Red" => _redNoteScene,
            "Yellow" => _yellowNoteScene,
            "Green" => _greenNoteScene,
            "Blue" => _blueNoteScene,
            _ => null,
        };

        // x position by color
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
        var noteInstance = (Node2D)noteScene.Instantiate();
        var noteMovement = noteInstance.GetNode<NoteMovement>("Area2D");

        if (noteMovement != null)
        {
            noteMovement.NoteColor = color;
            noteInstance.Position = new Vector2(xPosition, yPosition);
            AddChild(noteInstance);
        }
        else
        {
            GD.PrintErr($"NoteMovement script not found in the instantiated scene for color {color}.");
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
