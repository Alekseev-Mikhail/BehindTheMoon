using System;
using Godot;

namespace BehindTheMoon.Util;

public class Animation(float duration)
{
    public Vector3 Source;
    public Vector3 Target;
    public Action Update;
    private bool _isRunning;
    
    private float _elapsedTime;

    public void Start()
    {
        Update?.Invoke();
        _isRunning = true;
    }

    public void Stop()
    {
        _elapsedTime = 0f;
        _isRunning = false;
    }
    
    public Vector3 Step(Vector3 current, float deltaTime)
    {
        if (!_isRunning) return current; 
        _elapsedTime += deltaTime;
        var weight = _elapsedTime / duration; 
        var newCurrent = new Vector3(
            CosineInterpolate(Source.X, Target.X, weight),
            CosineInterpolate(Source.Y, Target.Y, weight),
            CosineInterpolate(Source.Z, Target.Z, weight)
        );
        if (!(_elapsedTime > duration)) return newCurrent;
        _elapsedTime = 0f;
        Update?.Invoke();
        return newCurrent;
    }
    
    private static float CosineInterpolate(float from, float to, float weight)
    {
        var weight2 = (1 - Mathf.Cos(weight * Mathf.Pi)) / 2;
        return from * (1 - weight2) + to * weight2;
    }
}