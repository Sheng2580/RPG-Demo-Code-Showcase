using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicManager : UnitySingleTonMono<MusicManager>
{
    private AudioSource bkMusic;
    private float bkVolume = 1;
    private float soundVolume = 1;
    private Coroutine bgMusicTransitionCoroutine;
    private string currentBkMusicName;
    private int bgMusicRequestId;
    private const float DefaultBGMusicFadeDuration = 1f;

    private List<AudioSource> soundlist = new List<AudioSource>();

    public void PlayBGMusic(string name)
    {
        AudioClip clip = ResourceManager.Instance.load<AudioClip>("Music/BG/" + name);
        if (clip == null)
        {
            Debug.LogError("[MusicManager] Load bgm failed: " + name);
            return;
        }

        SwitchBGMusic(clip, "Resources:" + name, DefaultBGMusicFadeDuration);
    }


    public void PlayBGMusicForAB(string name)
    {
        int requestId = ++bgMusicRequestId;
        ABManager.Instance.LoadResAsync<AudioClip>("bgm", name, clip =>
        {
            if (requestId != bgMusicRequestId)
            {
                return;
            }

            if (clip == null)
            {
                Debug.LogError("[MusicManager] Load bgm failed: " + name);
                return;
            }

            SwitchBGMusic(clip, "AB:bgm/" + name, DefaultBGMusicFadeDuration);
        });
    }


    public void StopBKMusic()
    {
        if (bkMusic == null) return;
        StopBGMusicTransition();
        currentBkMusicName = null;
        bkMusic.Stop();
    }

    public void PauseBKMusic()
    {
        if (bkMusic == null) return;
        bkMusic.Pause();
    }

    public void changeBkVolume(float volume)
    {
        bkVolume = volume;
        if (bkMusic == null) return;
        bkMusic.volume = volume;
    }

    private void SwitchBGMusic(AudioClip clip, string musicName, float fadeDuration)
    {
        EnsureBGMusicSource();
        if (bkMusic == null || clip == null)
        {
            return;
        }

        if (currentBkMusicName == musicName && bkMusic.clip == clip && bkMusic.isPlaying)
        {
            bkMusic.volume = bkVolume;
            return;
        }

        StopBGMusicTransition();
        bgMusicTransitionCoroutine = StartCoroutine(SwitchBGMusicCoroutine(clip, musicName, fadeDuration));
    }

    private IEnumerator SwitchBGMusicCoroutine(AudioClip clip, string musicName, float fadeDuration)
    {
        fadeDuration = Mathf.Max(0f, fadeDuration);
        float fadeOutDuration = bkMusic.isPlaying ? fadeDuration * 0.5f : 0f;
        float fadeInDuration = fadeDuration * 0.5f;

        if (fadeOutDuration > 0f)
        {
            float startVolume = bkMusic.volume;
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                if (bkMusic == null)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                bkMusic.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeOutDuration);
                yield return null;
            }
        }

        if (bkMusic == null)
        {
            yield break;
        }

        bkMusic.Stop();
        bkMusic.clip = clip;
        bkMusic.loop = true;
        bkMusic.volume = fadeInDuration > 0f ? 0f : bkVolume;
        bkMusic.Play();
        currentBkMusicName = musicName;

        if (fadeInDuration > 0f)
        {
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                if (bkMusic == null)
                {
                    yield break;
                }

                elapsed += Time.unscaledDeltaTime;
                bkMusic.volume = Mathf.Lerp(0f, bkVolume, elapsed / fadeInDuration);
                yield return null;
            }
        }

        if (bkMusic != null)
        {
            bkMusic.volume = bkVolume;
        }

        bgMusicTransitionCoroutine = null;
    }

    private void EnsureBGMusicSource()
    {
        if (bkMusic != null)
        {
            return;
        }

        GameObject obj = new GameObject("BGMusic");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;
        bkMusic = obj.AddComponent<AudioSource>();
        bkMusic.playOnAwake = false;
    }

    private void StopBGMusicTransition()
    {
        if (bgMusicTransitionCoroutine == null)
        {
            return;
        }

        StopCoroutine(bgMusicTransitionCoroutine);
        bgMusicTransitionCoroutine = null;
    }

    public void PlaySound(string soundName, bool isLoop = false)
    {
        GameObject soundObj = PoolManager.Instance.getObj("Music/Sound/" + soundName);
        AudioSource source = soundObj.GetComponent<AudioSource>();
        if (soundObj.GetComponent<AudioSource>() == null) 
            source = soundObj.AddComponent<AudioSource>();
        source.clip = ResourceManager.Instance.load<AudioClip>("Music/Sound/" + soundName);
        source.volume = soundVolume;
        source.loop = isLoop;
        source.Play();
        soundlist.Add(source);
    }

    public void PlaySoundForAB(string soundName,string abName = "sound", bool isLoop = false)
    {
        PlaySoundForABInternal(soundName, null, abName, isLoop);
    }

    public void PlaySoundForAB(string soundName, Vector3 worldPosition, string abName = "sound", bool isLoop = false)
    {
        PlaySoundForABInternal(soundName, worldPosition, abName, isLoop);
    }

    private void PlaySoundForABInternal(string soundName, Vector3? worldPosition, string abName, bool isLoop)
    {
        if (string.IsNullOrEmpty(soundName))
        {
            return;
        }

        GameObject soundObj = PoolManager.Instance.getObj(soundName);
        if (worldPosition.HasValue)
        {
            soundObj.transform.position = worldPosition.Value;
        }

        AudioSource source = soundObj.GetComponent<AudioSource>();
        if (soundObj.GetComponent<AudioSource>() == null) 
            source = soundObj.AddComponent<AudioSource>();
        source.spatialBlend = worldPosition.HasValue ? 1f : 0f;
        source.clip = ResourceManager.Instance.load<AudioClip>("Music/Sound/" + soundName);
         ABManager.Instance.LoadResAsync(abName,soundName,typeof(AudioClip), (obj) =>
        {
            if (source == null)
            {
                return;
            }

            AudioClip clip = obj as AudioClip;
            if (clip == null)
            {
                Debug.LogError("[MusicManager] Load sound failed: " + soundName);
                PoolManager.Instance.pushObj(soundName, soundObj);
                return;
            }

            source.clip = clip;
            source.volume = soundVolume;
            source.loop = isLoop;
            source.Play();
            soundlist.Add(source);
        });
    }

    public void StopSound(string soundName, AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        soundName = "Music/Sound/" + soundName;
        if (soundlist.Contains(source))
        {
            soundlist.Remove(source);
            source.Stop();
            PoolManager.Instance.pushObj(soundName, source.gameObject);
        }
    }

    public void ChangeSoundVolume(float volume)
    {
        soundVolume = volume;
        for (int i = soundlist.Count - 1; i >= 0; i--)
        {
            if (soundlist[i] == null)
            {
                soundlist.RemoveAt(i);
                continue;
            }

            soundlist[i].volume = soundVolume;
        }
    }

    private void Update()
    {
        if (soundlist.Count == 0) return;
        for (int i = soundlist.Count - 1; i >= 0; i--)
        {
            if (soundlist[i] == null)
            {
                soundlist.RemoveAt(i);
                continue;
            }

            string soundName = soundlist[i].name;
            if (!soundlist[i].isPlaying)
            {
                PoolManager.Instance.pushObj(soundName, soundlist[i].gameObject);
                soundlist.RemoveAt(i);
            }
        }
    }
}


