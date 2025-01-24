using Godot;
using System;

public partial class NoteMovement : Area2D
{
    private float movementMultiplier = 6;

    // Add a property to specify the note's color
    [Export] public string NoteColor { get; set; }

    public override void _Ready()
    {
        Name = $"Note_{GetInstanceId()}";
    }

    public override void _PhysicsProcess(double delta)
    {
        MoveNote(1);
    }

    private void MoveNote(double speed)
    {
        Position += new Vector2(0, movementMultiplier * (float)speed);

        if (Position.Y > 4000)
        {
            GD.Print($"Note missed: {Name}");
            QueueFree();
        }
    }

    public async void DestroyNote()
    {
        await ToSignal(GetTree().CreateTimer(1.0f), "timeout"); // Wait 1 second
        QueueFree();
    }

}