using System;
using Godot;

namespace BehindTheMoon.Util;

public class Animation(float duration)
{
    public Vector3 Source;
    public Vector3 Target;
    
    public Action Update;
    
    private float _elapsedTime;

    public void Start()
    {
        Update?.Invoke();
    }
    
    public Vector3 Step(float deltaTime)
    {
        _elapsedTime += deltaTime;
        var current = Source.Lerp(Target, _elapsedTime / duration);
        if (!(_elapsedTime > duration)) return current;
        _elapsedTime = 0f;
        Update?.Invoke();
        return current;
    }
}