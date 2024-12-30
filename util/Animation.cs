using System;
using Godot;

namespace BehindTheMoon.Util;

public class Animation(Curve curve, float duration, bool repeatable)
{
    public Vector3 Source;
    public Vector3 Max;
    public Action Update;
    public Action Stopped;
    
    private Vector3 _delta;
    private bool _isRunning;
    private float _elapsedTime;

    public void Start()
    {
        Update?.Invoke();
        _delta = Max - Source;
        _isRunning = true;
    }

    public void Stop()
    {
        _elapsedTime = 0f;
        _isRunning = false;
        Stopped?.Invoke();
    }

    public Vector3 Step(Vector3 current, float deltaTime)
    {
        if (!_isRunning) return new Vector3();
        _elapsedTime += deltaTime;
        var offset = _elapsedTime / duration;
        var sample = curve.Sample(offset);
        var newCurrentDelta = Source + sample * _delta - current;
        if (!(_elapsedTime > duration)) return newCurrentDelta;
        if (repeatable)
        {
            _elapsedTime = 0f;
            Update?.Invoke();
        }
        else
        {
            Stop();
        }

        return newCurrentDelta;
    }
}