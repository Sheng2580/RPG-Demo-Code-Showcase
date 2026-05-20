using UnityEngine;
using UnityEngine.Playables;
public class TimeLineManager : UnitySingleTonMono<TimeLineManager>
{
    public void PlayTimeLine(TimeLineBase timeLineBase)
    {
        if (timeLineBase == null)
        {
            Debug.LogError("[TimeLineManager] TimeLineBase is null, play failed.");
            return;
        }

        PlayableDirector playableDirector = timeLineBase.PlayableDirector;
        if (playableDirector == null)
        {
            Debug.LogError($"[TimeLineManager] {timeLineBase.name} has no PlayableDirector.");
            return;
        }

        if (timeLineBase.StopBeforePlay)
        {
            ResetTimeLine(playableDirector);
        }

        playableDirector.Play();
    }

    public void StopTimeLine(TimeLineBase timeLineBase)
    {
        if (timeLineBase?.PlayableDirector == null)
        {
            return;
        }

        ResetTimeLine(timeLineBase.PlayableDirector);
    }

    public void ResetTimeLine(PlayableDirector playableDirector)
    {
        if (playableDirector == null)
        {
            return;
        }

        playableDirector.Stop();
        playableDirector.time = 0;
        playableDirector.Evaluate();
    }
}


