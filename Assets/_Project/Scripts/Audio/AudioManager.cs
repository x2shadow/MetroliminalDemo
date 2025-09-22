using UnityEngine;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        public bool loop;
        [HideInInspector] public AudioSource source;
    }

    [Header("Настройки звуков")]
    [SerializeField] private Sound[] musicTracks;
    [SerializeField] private Sound[] soundEffects;

    [Header("Источники звука")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private int initialPoolSize = 10;

    private Dictionary<string, Sound> soundDictionary = new Dictionary<string, Sound>();
    private Queue<AudioSource> soundPool = new Queue<AudioSource>();
    private float masterMusicVolume = 0.5f;
    private float masterSoundVolume = 0.5f;
    private Sound currentMusicTrack;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    void Initialize()
    {
        // Загрузка сохраненных настроек громкости
        masterMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 1f);
        masterSoundVolume = PlayerPrefs.GetFloat("SoundVolume", 1f);

        // Инициализация пула источников звука
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewSoundSource();
        }

        // Инициализация музыки
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        // Заполнение словаря звуков
        foreach (var track in musicTracks)
        {
            soundDictionary.Add(track.name, track);
        }

        foreach (var sound in soundEffects)
        {
            soundDictionary.Add(sound.name, sound);
        }
    }

    AudioSource CreateNewSoundSource()
    {
        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        newSource.playOnAwake = false;
        soundPool.Enqueue(newSource);
        return newSource;
    }

    public void PlayMusic(string trackName)
    {
        if (!soundDictionary.ContainsKey(trackName))
        {
            Debug.LogWarning($"Музыкальный трек {trackName} не найден!");
            return;
        }

        Sound track = soundDictionary[trackName];
        currentMusicTrack = track;
        musicSource.clip = track.clip;
        musicSource.volume = track.volume * masterMusicVolume;
        musicSource.pitch = track.pitch;
        musicSource.loop = track.loop;
        musicSource.Play();
    }

    public void PlaySound(string soundName)
    {
        if (!soundDictionary.ContainsKey(soundName))
        {
            Debug.LogWarning($"Звук {soundName} не найден!");
            return;
        }

        Sound sound = soundDictionary[soundName];
        AudioSource source = GetAvailableSoundSource();

        source.clip = sound.clip;
        source.volume = sound.volume * masterSoundVolume;
        source.pitch = sound.pitch;
        source.loop = sound.loop;
        source.Play();

        if (!sound.loop)
        {
            StartCoroutine(ReturnToPoolAfterPlay(source, sound.clip.length));
        }
    }

    public void PlaySound(AudioClip soundClip)
    {
        AudioSource source = GetAvailableSoundSource();

        source.clip = soundClip;
        //source.volume = soundClip.volume * masterSoundVolume;
        source.volume = 1f * masterSoundVolume;
        //source.pitch = soundClip.pitch;
        source.loop = false;
        source.Play();

        if (!source.loop)
        {
            StartCoroutine(ReturnToPoolAfterPlay(source, soundClip.length));
        }
    }

    private System.Collections.IEnumerator ReturnToPoolAfterPlay(AudioSource source, float duration)
    {
        yield return new WaitForSeconds(duration);
        if (source != null && !source.loop)
        {
            source.Stop();
            source.clip = null;
            soundPool.Enqueue(source);
        }
    }

    AudioSource GetAvailableSoundSource()
    {
        foreach (AudioSource source in soundPool)
        {
            if (!source.isPlaying)
            {
                return source;
            }
        }

        // Если все источники заняты - создать новый
        return CreateNewSoundSource();
    }

    public void SetMusicVolume(float volume)
    {
        masterMusicVolume = Mathf.Clamp01(volume);

        musicSource.volume = currentMusicTrack.volume * masterMusicVolume;
        PlayerPrefs.SetFloat("MusicVolume", masterMusicVolume);
    }

    public void SetSoundVolume(float volume)
    {
        masterSoundVolume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat("SoundVolume", masterSoundVolume);
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void StopAllSound()
    {
        foreach (AudioSource source in soundPool)
        {
            source.Stop();
            source.clip = null;
        }
    }
    
    public float GetMusicVolume()
    {
        return masterMusicVolume;
    }

    public float GetSoundVolume()
    {
        return masterSoundVolume;
    }
}