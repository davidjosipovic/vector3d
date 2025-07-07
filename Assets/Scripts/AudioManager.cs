using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    
    [Header("Audio Sources")]
    public AudioSource musicSource;
    public AudioSource sfxSource;
    
    [Header("Music Clips")]
    public AudioClip mainMenuMusic;
    public AudioClip gameplayMusic;
    
    [Header("Settings")]
    [Range(0f, 1f)] public float musicVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    public bool fadeTransitions = true;
    public float fadeDuration = 1f;
    
    private AudioClip currentMusic;
    private bool isFading = false;
    
    void Awake()
    {
        // Singleton pattern - samo jedan AudioManager može da postoji
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Automatski pokreni main menu muziku
        PlayMainMenuMusic();
    }
    
    private void InitializeAudio()
    {
        // Kreiraj AudioSource komponente ako ne postoje
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }
        
        // Postavi volume
        musicSource.volume = musicVolume;
        sfxSource.volume = sfxVolume;
        
        Debug.Log("AudioManager initialized successfully!");
    }
    
    public void PlayMainMenuMusic()
    {
        if (mainMenuMusic != null)
        {
            PlayMusic(mainMenuMusic);
            Debug.Log("🎵 Playing main menu music");
        }
        else
        {
            Debug.LogWarning("Main menu music clip not assigned!");
        }
    }
    
    public void PlayGameplayMusic()
    {
        if (gameplayMusic != null)
        {
            PlayMusic(gameplayMusic);
            Debug.Log("🎵 Playing gameplay music");
        }
        else
        {
            Debug.LogWarning("Gameplay music clip not assigned!");
        }
    }
    
    public void PlayMusic(AudioClip musicClip)
    {
        if (musicClip == null) return;
        
        // Ako je ista muzika već pustena, ne radi ništa
        if (currentMusic == musicClip && musicSource.isPlaying)
        {
            return;
        }
        
        currentMusic = musicClip;
        
        if (fadeTransitions && musicSource.isPlaying)
        {
            // Fade transition između muzika
            StartCoroutine(FadeToNewMusic(musicClip));
        }
        else
        {
            // Direktno promeni muziku
            musicSource.clip = musicClip;
            musicSource.Play();
        }
    }
    
    private System.Collections.IEnumerator FadeToNewMusic(AudioClip newMusic)
    {
        if (isFading) yield break;
        
        isFading = true;
        float startVolume = musicSource.volume;
        
        // Fade out trenutna muzika
        while (musicSource.volume > 0)
        {
            musicSource.volume -= startVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }
        
        // Promeni muziku
        musicSource.Stop();
        musicSource.clip = newMusic;
        musicSource.Play();
        
        // Fade in nova muzika
        while (musicSource.volume < startVolume)
        {
            musicSource.volume += startVolume * Time.deltaTime / fadeDuration;
            yield return null;
        }
        
        musicSource.volume = startVolume;
        isFading = false;
    }
    
    public void StopMusic()
    {
        musicSource.Stop();
        currentMusic = null;
        Debug.Log("🔇 Music stopped");
    }
    
    public void PauseMusic()
    {
        musicSource.Pause();
        Debug.Log("⏸️ Music paused");
    }
    
    public void ResumeMusic()
    {
        musicSource.UnPause();
        Debug.Log("▶️ Music resumed");
    }
    
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        musicSource.volume = musicVolume;
        Debug.Log($"🔊 Music volume set to: {musicVolume:F2}");
    }
    
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        sfxSource.volume = sfxVolume;
        Debug.Log($"🔊 SFX volume set to: {sfxVolume:F2}");
    }
    
    public void PlaySFX(AudioClip sfxClip)
    {
        if (sfxClip != null)
        {
            sfxSource.PlayOneShot(sfxClip);
        }
    }
    
    // Debug metode za testiranje
    void Update()
    {
        // Debug keys za testiranje muzike
        if (Input.GetKeyDown(KeyCode.F1))
        {
            PlayMainMenuMusic();
        }
        
        if (Input.GetKeyDown(KeyCode.F2))
        {
            PlayGameplayMusic();
        }
        
        if (Input.GetKeyDown(KeyCode.F3))
        {
            StopMusic();
        }
        
        if (Input.GetKeyDown(KeyCode.F4))
        {
            if (musicSource.isPlaying)
                PauseMusic();
            else
                ResumeMusic();
        }
    }
    
    void OnValidate()
    {
        // Ažuriraj volume u real-time u editoru
        if (musicSource != null)
            musicSource.volume = musicVolume;
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }
}
