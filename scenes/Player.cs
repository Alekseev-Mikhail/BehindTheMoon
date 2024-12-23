using Godot;

namespace BehindTheMoon.scenes;

public partial class Player : CharacterBody3D
{
    public bool IsClient = true;
    [Export] private float _speed;
    [Export] private float _sensitivity;

    private Node3D _pivot;

    public override void _Ready()
    {
        if (!IsClient) return;
        _pivot = GetNode<Node3D>("Pivot");
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Process(double delta)
    {
        if (!IsClient) return;
        if (!Input.IsKeyPressed(Key.Escape)) return;
        if (Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            return;
        }
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsClient) return;
        var input = Input.GetVector("player_left", "player_right", "player_forward", "player_back");
        input *= _speed;
        Velocity = new Vector3(input.X, 0f, input.Y);
        Velocity = Velocity.Rotated(Vector3.Up, _pivot.Rotation.Y);
        MoveAndSlide();
        Rpc(MethodName.RemoteSetPosition, GlobalPosition);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void RemoteSetPosition(Vector3 position)
    {
        GlobalPosition = position;
    }
    
    public override void _Input(InputEvent @event)
    {
        if (!IsClient) return;
        if (@event is not InputEventMouseMotion mouseMotion) return;
        
        var input = mouseMotion.Relative;
        input *= _sensitivity;
        _pivot.RotateObjectLocal(Vector3.Left, input.Y);
        _pivot.RotateY(-input.X);
    }
    
    public Camera3D GetCamera() => GetNode<Camera3D>("Pivot/Camera3D");
}