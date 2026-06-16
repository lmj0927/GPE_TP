using UnityEngine;
using AYellowpaper.SerializedCollections;

public class AudioManager : Singleton<AudioManager>
{
    [SerializeField] private SerializedDictionary<AudioType, AudioClip> _audioClips;
    [SerializeField] private AudioSource _backgroundAudioSource;
    [SerializeField] private AudioSource _effectAudioSource;

    public static void TryPlay(AudioType audioType)
    {
        var manager = FindFirstObjectByType<AudioManager>();
        manager?.PlayAudio(audioType);
    }

    public void PlayAudio(AudioType audioType)
    {
        if (!_audioClips.TryGetValue(audioType, out var audioClip) || audioClip == null)
            return;

        if (audioType == AudioType.Intro || audioType == AudioType.Main || audioType == AudioType.Play)
            PlayBgm(audioClip);
        else if (_effectAudioSource != null)
            _effectAudioSource.PlayOneShot(audioClip);
    }

    private void PlayBgm(AudioClip clip)
    {
        if (_backgroundAudioSource == null)
            return;

        if (_backgroundAudioSource.clip == clip && _backgroundAudioSource.isPlaying)
            return;

        _backgroundAudioSource.Stop();
        _backgroundAudioSource.clip = clip;
        _backgroundAudioSource.loop = true;
        _backgroundAudioSource.Play();
    }
}

public enum AudioType
{
    Intro,
    Main,
    Play,
    Win,
    Lose,
    Hit,
    Spawn,
    Plus,
    Popup
}