using Godot;
using System;
using System.Collections.Generic;

public partial class NotePressed : Area2D
{
    // Map note colors to their respective input actions
    private Dictionary<string, StringName> _noteKeyMap = new Dictionary<string, StringName>
    {
        { "Red", "note_red" },
        { "Yellow", "note_yellow" },
        { "Green", "note_green" },
        { "Blue", "note_blue" }
    };

    // Track overlapping notes
    private List<NoteMovement> _overlappingNotes = new List<NoteMovement>();

    private RichTextLabel _scoreText;
    private float _score = 0;
    private int _mult = 1;
    private int _combo = 0;
    
    private Sprite2D _centerLine;
    private float _perfectRange = 15f;
    private float _greatRange = 64f;
    private float _goodRange = 128f;
    public override void _Ready()
    {
        Connect("area_entered", new Callable(this, nameof(OnNoteEntered)));
        Connect("area_exited", new Callable(this, nameof(OnNoteExited)));
        _scoreText = GetNode<RichTextLabel>("../../Control/ScoreText");
        _centerLine = GetNode<Sprite2D>("../Center line");
        GD.Print("y: " + _centerLine.GlobalPosition.Y);
    }

    private void OnNoteEntered(Node area)
    {
        if (area is NoteMovement note)
        {
            _overlappingNotes.Insert(0,note);
        }
    }

    private void OnNoteExited(Node area)
    {
        if (area is NoteMovement note)
        {
            _overlappingNotes.Remove(note);
            note.DestroyNote();
        }
    }

    public override void _Process(double delta)
    {
        foreach (var inputAction in _noteKeyMap)
        {
            if (Input.IsActionJustPressed(inputAction.Value))
            {
                ProcessInput(inputAction.Key);
            }
        }
    }

    private void ProcessInput(string noteColor)
    {
        // current note
        NoteMovement matchedNote = null;

        //cycle through the notes overlapping the rhythm bar
        // matchedNote will either be the note in overlappingNotes, or null
        foreach (var note in _overlappingNotes)
        {
            if (note.NoteColor == noteColor)
            {
                matchedNote = note;
            }
        }

        // if note was hit
        if (matchedNote != null)
        {
            HitNote(DetectDistance(matchedNote.GlobalPosition.Y));
            GD.Print($"Note hit: {noteColor}");
            matchedNote.QueueFree();
            _overlappingNotes.Remove(matchedNote);
            // Future: Trigger scoring and combo logic here
        }
        // if note was missed
        else
        {
            GD.Print($"Missed note for input: {noteColor}");
            // Future: Trigger miss logic here
            MissedNote();
        }
    }

    private void HitNote(string distance)
    {
        var _perfectGreatGoodMult = 0f;
        switch (distance)
        {
            case "Perfect!":
                _perfectGreatGoodMult = 2f;
                break;
            case "Great!":
                _perfectGreatGoodMult = 1.5f;
                break;
            case "Good!":
                _perfectGreatGoodMult = 1f;
                break;
        }
        
        _score += _mult * _perfectGreatGoodMult;
        _combo++;
        switch (_combo)
        {
            case 1:
                _mult = 1;
                break;
            case 10:
                _mult = 2;
                break;
            case 20:
                _mult = 3;
                break;
            case 30:
                _mult = 4;
                break;
        }
        UpdateScoreAndMult(distance);
    }

    private void MissedNote()
    {
        _combo = 0;
        UpdateScoreAndMult("Missed!");
    }

    private void UpdateScoreAndMult(string distance)
    {
        _scoreText.Text = "[center]" + distance
            + "\nScore:\n" + _score.ToString()
            + "\nCombo:\n" + _combo.ToString()
            + "\nMultiplier:\n" + _mult.ToString();
    }

    private string DetectDistance(float noteY)
    {
        var dist = Mathf.Abs(noteY - _centerLine.GlobalPosition.Y);
        GD.Print("noteY: " + noteY);
        GD.Print("Distance: " + dist);
        if (dist < _perfectRange)
        {
            return "Perfect!";
        }
        else if (dist < _greatRange)
        {
            return "Great!";
        }
        else
        {
            return "Good!";
        }
    }
}
