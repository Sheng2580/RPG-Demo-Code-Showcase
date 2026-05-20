using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public enum TimerState
{
    Running,
    Paused,
    Stopped
}

public class Timer
{
    public string Id { get; private set; }
    public float CurrentTime { get; private set; }
    public float TargetTime { get; private set; }
    public TimerState State { get; private set; }
    public bool UseRealtime { get; private set; }

    public event Action<float> OnUpdate;
    public event Action OnTimeUp;

    public Timer(string id, bool useRealtime = false)
    {
        Id = id;
        UseRealtime = useRealtime;
        CurrentTime = 0f;
        TargetTime = -1f;
        State = TimerState.Stopped;
    }

    internal void Update()
    {
        if (State != TimerState.Running) return;

        float deltaTime = UseRealtime ? Time.unscaledDeltaTime : Time.deltaTime;
        CurrentTime += deltaTime;

        OnUpdate?.Invoke(CurrentTime);

        if (TargetTime > 0 && CurrentTime >= TargetTime)
        {
            Stop();
            OnTimeUp?.Invoke();
        }
    }

    public void Start(bool reset = true)
    {
        if (reset)
        {
            CurrentTime = 0f;
        }
        State = TimerState.Running;
    }

    public void Pause()
    {
        if (State == TimerState.Running)
        {
            State = TimerState.Paused;
        }
    }

    public void Resume()
    {
        if (State == TimerState.Paused)
        {
            State = TimerState.Running;
        }
    }

    public void Stop()
    {
        State = TimerState.Stopped;
    }

    public void Reset()
    {
        CurrentTime = 0f;
        State = TimerState.Stopped;
    }

    public void SetTargetTime(float target)
    {
        if (target < 0)
        {
            Debug.LogWarning($"璁℃椂鍣╗{Id}]锛氱洰鏍囨椂闂翠笉鑳戒负璐熸暟");
            return;
        }
        TargetTime = target;
    }

    public string GetFormattedTime()
    {
        int minutes = Mathf.FloorToInt(CurrentTime / 60);
        float secondsRaw = CurrentTime % 60;
        int seconds = Mathf.FloorToInt(secondsRaw);
        int milliseconds = Mathf.FloorToInt((secondsRaw - seconds) * 1000);

        return $"{minutes:D2}:{seconds:D2}.{milliseconds:D3}";
    }

    public void ClearEvents()
    {
        OnUpdate = null;
        OnTimeUp = null;
    }
}

public class MultiTimerManager : UnitySingleTonMono<MultiTimerManager>
{
    private Dictionary<string, Timer> timers = new Dictionary<string, Timer>();

    private void Update()
    {
        foreach (var timer in timers.Values.ToArray()) 
        {
            timer.Update();
        }
    }

    public Timer CreateTimer(string timerId, bool useRealtime = true)
    {
        if (timers.ContainsKey(timerId))
        {
            RemoveTimer(timerId);
        }

        Timer newTimer = new Timer(timerId, useRealtime);
        timers.Add(timerId, newTimer);
        return newTimer;
    }

    public Timer GetTimer(string timerId)
    {
        if (timers.TryGetValue(timerId, out Timer timer))
        {
            return timer;
        }
        Debug.LogWarning($"璁℃椂鍣╗{timerId}]涓嶅瓨鍦?);
        return null;
    }

    public void RemoveTimer(string timerId)
    {
        if (timers.TryGetValue(timerId, out Timer timer))
        {
            timer.ClearEvents();
            timers.Remove(timerId);
        }
        else
        {
            Debug.LogWarning($"绉婚櫎澶辫触锛氳鏃跺櫒[{timerId}]涓嶅瓨鍦?);
        }
    }

    public void RemoveAllTimers()
    {
        foreach (var timer in timers.Values)
        {
            timer.ClearEvents();
        }
        timers.Clear();
    }

    public void AddOneShotTimer(float delay, Action callback, bool useRealtime = true)
    {
        if (delay < 0)
        {
            Debug.LogWarning("寤惰繜鏃堕棿涓嶈兘涓鸿礋鏁?);
            return;
        }
        if (callback == null)
        {
            Debug.LogWarning("鍥炶皟鍑芥暟涓嶈兘涓虹┖");
            return;
        }

        string tempId = $"OneShot_{Guid.NewGuid().ToString().Substring(0, 8)}";

        Timer tempTimer = CreateTimer(tempId, useRealtime);
        tempTimer.SetTargetTime(delay);
        tempTimer.OnTimeUp += () =>
        {
            callback.Invoke();
            RemoveTimer(tempId);
        };
        tempTimer.Start();
    }

}



