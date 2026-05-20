using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class IEventInfoMY
{
}

public class MyEventInfoMy : IEventInfoMY
{
    public UnityAction actions;

    public MyEventInfoMy(UnityAction action)
    {
        actions += action;
    }
}

public class EventInfoMy<T> : IEventInfoMY
{
    public UnityAction<T> actions;

    public EventInfoMy(UnityAction<T> action)
    {
        actions += action;
    }
}


public class EventCenter : SingleTon<EventCenter>
{
    public Dictionary<GameEvent, IEventInfoMY> eventDict = new Dictionary<GameEvent, IEventInfoMY>();

    public void AddEventListener(GameEvent sGameEvent, UnityAction action)
    {
        if (eventDict.ContainsKey(sGameEvent))
        {
            (eventDict[sGameEvent] as MyEventInfoMy).actions += action;
        }
        else
        {
            eventDict.Add(sGameEvent, new MyEventInfoMy(action));
        }
    }

    public void AddEventListener<T>(GameEvent sGameEvent, UnityAction<T> action)
    {
        if (eventDict.ContainsKey(sGameEvent))
        {
            (eventDict[sGameEvent] as EventInfoMy<T>).actions += action;
        }
        else
        {
            eventDict.Add(sGameEvent, new EventInfoMy<T>(action));
        }
    }

    public void RemoveEventListener(GameEvent sGameEvent, UnityAction action)
    {
        if (eventDict.ContainsKey(sGameEvent))
        {
            (eventDict[sGameEvent] as MyEventInfoMy).actions -= action;
        }
    }

    public void RemoveEventListener<T>(GameEvent sGameEvent, UnityAction<T> action)
    {
        if (eventDict.ContainsKey(sGameEvent))
        {
            (eventDict[sGameEvent] as EventInfoMy<T>).actions -= action;
        }
    }
    public void EventTrigger(GameEvent sGameEvent)
    {
        if (eventDict.ContainsKey(sGameEvent))
        {
            (eventDict[sGameEvent] as MyEventInfoMy).actions?.Invoke();  
        }

    }
public void EventTrigger<T>(GameEvent sGameEvent,T info)
    {
        if (eventDict.ContainsKey(sGameEvent))
        {
            (eventDict[sGameEvent] as EventInfoMy<T>).actions?.Invoke(info);  
        }

    }

    public void Clear()
    {
        eventDict.Clear();
    }


}

