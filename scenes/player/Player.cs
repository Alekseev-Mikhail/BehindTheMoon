using Godot;

namespace BehindTheMoon.scenes.player;

public partial class Player : CharacterBody3D
{
    [Export]
    public float Speed;
    [Export]
    public float Sensitivity;
    
    private Node3D _pivot;

    public override void _Ready()
    {
        _pivot = GetNode<Node3D>("Pivot");
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }
    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseMotion mouseMotion) return;
        
        var input = mouseMotion.Relative;
        input *= Sensitivity;
        _pivot.RotateObjectLocal(Vector3.Left, input.Y);
        _pivot.RotateY(-input.X);
    }

    public override void _PhysicsProcess(double delta)
    {
        var input = Input.GetVector("player_left", "player_right", "player_forward", "player_back");
        input *= Speed;
        Velocity = new Vector3(input.X, 0f, input.Y);
        Velocity = Velocity.Rotated(Vector3.Up, _pivot.Rotation.Y);
        MoveAndSlide();
    }
}