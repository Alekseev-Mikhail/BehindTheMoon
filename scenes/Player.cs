using Godot;
using static Godot.GeometryInstance3D;

namespace BehindTheMoon.scenes;

public partial class Player : CharacterBody3D
{
    public bool IsClient = true;
    private Node3D _pivot;
    private Node3D _headPivot;
    private bool _isRunning;
    
    [Export] private float _basicSpeed;
    [Export] private float _sensitivity;
    [Export] private float _fallingAcceleration;
    [Export] private float _jumpForce;
    [Export] private float _runMultiplier;

    public override void _Ready()
    {
        _pivot = GetNode<Node3D>("Pivot");
        _headPivot = GetNode<Node3D>("Pivot/HeadPivot");
        if (!IsClient) return;
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
        ProcessRunningState();
        AddMovementForceIfNeed();
        AddFallingForceIfNeed();
        AddJumpingForceIfNeed();
        MoveAndSlide();
        Rpc(MethodName.RemoteSetPosition, GlobalPosition);
    }

    private void ProcessRunningState()
    {
        if (Input.IsActionJustReleased("player_run") || !IsOnFloor()) _isRunning = false;
        else if (Input.IsActionPressed("player_run")) _isRunning = true;
    }
    
    private void AddMovementForceIfNeed()
    {
        var input = Input.GetVector("player_left", "player_right", "player_forward", "player_back");
        var newVelocity = new Vector3(input.X, 0f, input.Y) * _basicSpeed;
        if (_isRunning) newVelocity *= _runMultiplier;
        newVelocity.Y = Velocity.Y;
        newVelocity = newVelocity.Rotated(Vector3.Up, _pivot.Rotation.Y);
        Velocity = newVelocity;
    }

    private void AddFallingForceIfNeed()
    {
        if (IsOnFloor()) return;
        Velocity += new Vector3(0f, -_fallingAcceleration, 0f);
    }

    private void AddJumpingForceIfNeed()
    {
        if (!IsOnFloor()) return;
        if (!Input.IsActionPressed("player_jump")) return;
        Velocity += new Vector3(0f, _jumpForce, 0f);
    }

    public override void _Input(InputEvent @event)
    {
        if (!IsClient) return;
        if (@event is not InputEventMouseMotion mouseMotion) return;
        
        var input = mouseMotion.Relative;
        input *= _sensitivity;
        _headPivot.RotateX(-input.Y);
        _pivot.RotateY(-input.X);
        Rpc(MethodName.RemoteSetRotation, _pivot.Rotation, _headPivot.Rotation);
    }
    
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void RemoteSetPosition(Vector3 position)
    {
        GlobalPosition = position;
    }
    
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void RemoteSetRotation(Vector3 pivotRotation, Vector3 headPivotRotation)
    {
        _pivot.SetRotation(pivotRotation);
        _headPivot.SetRotation(headPivotRotation);
    }

    public void SetInvisible()
    {
        GetNode<MeshInstance3D>("Pivot/Body/Main").CastShadow = ShadowCastingSetting.ShadowsOnly;
        GetNode<MeshInstance3D>("Pivot/Body/Nose").CastShadow = ShadowCastingSetting.ShadowsOnly;
    }

    public Camera3D GetCamera() => GetNode<Camera3D>("Pivot/HeadPivot/Camera3D");
}