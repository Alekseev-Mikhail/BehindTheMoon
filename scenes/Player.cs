using System;
using Godot;
using static Godot.GeometryInstance3D;
using Animation = BehindTheMoon.Util.Animation;

namespace BehindTheMoon.scenes;

public partial class Player : CharacterBody3D
{
    public bool IsClient = true;
    private Node3D _pivot;
    private Node3D _headPivot;
    private BoxShape3D _collisionShape;
    private Camera3D _camera;

    private Vector3 _defaultCollisionShapeSize;
    private Vector3 _collisionShapeSizeInCrouching;
    private Vector3 _defaultHeadPivotPosition;
    private Vector3 _headPivotPositionInCrouching;
    private Vector3 _defaultCameraPosition;

    private bool _isRunning;
    private bool _isCrouching;

    private Animation _swayPositionAnimation;
    private Animation _swayRotationAnimation;

    [Export, ExportGroup("Movement")] private float _basicSpeed;
    [Export] private float _sensitivity;
    [Export] private float _fallingAcceleration;
    [Export] private float _jumpForce;
    [Export] private float _runMultiplier;
    [Export] private float _crouchMultiplier;
    [Export] private float _crouchHeight;
    [Export, ExportGroup("Animations")] private float _swayDuration1;
    [Export] private float _swayEdgeLength1;
    private float _swayEdgeLengthHalf1;
    [Export] private float _SwayDuration2;
    [Export] private float _swayEdgeLength2;
    private float _swayEdgeLengthHalf2;

    public override void _Ready()
    {
        _pivot = GetNode<Node3D>("Pivot");
        _headPivot = GetNode<Node3D>("Pivot/HeadPivot");
        _collisionShape = GetNode<CollisionShape3D>("CollisionShape3D").Shape as BoxShape3D;
        _camera = GetNode<Camera3D>("Pivot/HeadPivot/Camera3D");

        if (_collisionShape == null) throw new InvalidCastException("CollisionShape is not a BoxShape3D");
        if (IsClient) Input.MouseMode = Input.MouseModeEnum.Captured;

        _defaultCollisionShapeSize = _collisionShape.Size;
        _collisionShapeSizeInCrouching = new Vector3(_collisionShape.Size.X, _crouchHeight, _collisionShape.Size.Z);

        _defaultHeadPivotPosition = _headPivot.Position;
        _headPivotPositionInCrouching = _headPivot.Position;
        _headPivotPositionInCrouching.Y -= (_defaultCollisionShapeSize.Y - _collisionShapeSizeInCrouching.Y) / 2;

        _defaultCameraPosition = _camera.Position;

        InitAnimations();
    }

    private void InitAnimations()
    {
        _swayPositionAnimation = new Animation(_swayDuration1);
        _swayEdgeLengthHalf1 = _swayEdgeLength1 * 0.5f;
        _swayPositionAnimation.Update = () =>
        {
            _swayPositionAnimation.Source = _camera.Position;
            _swayPositionAnimation.Target = GetRandomBox(_swayEdgeLength1, _swayEdgeLengthHalf1);
        };
        _swayPositionAnimation.Start();

        _swayRotationAnimation = new Animation(_SwayDuration2);
        _swayEdgeLengthHalf2 = _swayEdgeLength2 * 0.5f;
        _swayRotationAnimation.Update = () =>
        {
            _swayRotationAnimation.Source = _camera.Rotation;
            _swayRotationAnimation.Target = GetRandomBox(_swayEdgeLength2, _swayEdgeLengthHalf2);
        };
        _swayRotationAnimation.Start();
    }

    public override void _Process(double delta)
    {
        if (!IsClient) return;

        _camera.Position = _swayPositionAnimation.Step((float)delta);
        _camera.Rotation = _swayRotationAnimation.Step((float)delta);

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
        ProcessCrouchState();
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

    private void ProcessCrouchState()
    {
        if (_isRunning) _isCrouching = false;
        else if (Input.IsActionJustPressed("player_crouch") && IsOnFloor()) _isCrouching = !_isCrouching;
        if (_isCrouching)
        {
            _collisionShape.Size = _collisionShapeSizeInCrouching;
            _headPivot.Position = _headPivotPositionInCrouching;
        }
        else
        {
            _collisionShape.Size = _defaultCollisionShapeSize;
            _headPivot.Position = _defaultHeadPivotPosition;
        }
    }

    private void AddMovementForceIfNeed()
    {
        var input = Input.GetVector("player_left", "player_right", "player_forward", "player_back");
        var newVelocity = new Vector3(input.X, 0f, input.Y) * _basicSpeed;
        if (_isRunning) newVelocity *= _runMultiplier;
        else if (_isCrouching) newVelocity *= _crouchMultiplier;
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
        _isCrouching = false;
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

    private static Vector3 GetRandomBox(float edgeLength, float halfEdgeLength) => new(
        GD.Randf() * edgeLength - halfEdgeLength,
        GD.Randf() * edgeLength - halfEdgeLength,
        GD.Randf() * edgeLength - halfEdgeLength
    );

    public Camera3D GetCamera() => _camera;
}