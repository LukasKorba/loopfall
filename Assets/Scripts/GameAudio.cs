using UnityEngine;
using System.Runtime.InteropServices;

/// <summary>
/// Central audio manager — handles all game sound effects and voice lines.
/// Attach to a persistent GameObject in the scene.
///
/// Audio files go in Assets/Resources/Audio/ with these names:
///   Music:
///     mainTrack1.ogg — background music, loops continuously
///   Swing voices:
///     swing_tier1_0.ogg, swing_tier1_1.ogg, ... (e.g. "Nice swing", "Smooth", "Flow")
///     swing_tier2_0.ogg, swing_tier2_1.ogg, ... (e.g. "Unstoppable", "Gravity master")
///   SFX (future):
///     sfx_tap.ogg, sfx_gate.ogg, sfx_death.ogg, sfx_streak.ogg, etc.
/// </summary>
public class GameAudio : MonoBehaviour
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern bool _IsOtherAudioPlaying();
#endif

    private Torus mTorus;
    private Rigidbody mBallRb;
    private Sphere mSphere;
    private RewindSystem mRewind;

    // ── MUSIC ────────────────────────────────────────────────
    private bool externalMusicPlaying = false;
    private AudioSource musicSource;
    private AudioClip[] musicClips;
    private int lastMusicIndex = -1;
    private const float MUSIC_VOLUME = 0.2f;

    // ── SFX ──────────────────────────────────────────────────
    private AudioSource sfxSource;
    private AudioSource tapSource; // Dedicated so taps never get dropped
    private AudioClip gameOverClip;
    private AudioClip glitchClip;
    private AudioClip tapClip;
    private AudioClip countClip;
    private AudioClip gateClip;
    private AudioClip newBestClip;
    private AudioClip top5Clip;
    private AudioClip gateDissolveClip;
    private AudioClip gateSpawnClip;

    // ── REWIND SOUND ─────────────────────────────────────────
    private AudioSource rewindSource;
    private AudioClip rewindClip;
    private bool rewindPlaying = false;
    private float rewindFadeVel = 0f;
    private const float REWIND_VOLUME = 0.5f;
    private const float REWIND_FADE_OUT = 0.3f; // Fade duration in seconds

    // ── ROLLING SOUND ────────────────────────────────────────
    private AudioSource rollSource;
    private AudioClip rollClip;
    private float rollSmoothVel = 0f;  // For SmoothDamp
    private const float ROLL_MIN_VOLUME = 0.02f;
    private const float ROLL_MAX_VOLUME = 0.45f;
    private const float ROLL_MIN_PITCH = 0.7f;
    private const float ROLL_MAX_PITCH = 1.4f;
    private const float ROLL_SMOOTH_TIME = 0.15f;  // Smoothing speed
    private const float ROLL_VEL_MIN = 0.2f;   // Below this = silent
    private const float ROLL_VEL_MAX = 4.0f;   // At or above = max

    // ── SWING VOICE LINES ────────────────────────────────────
    private AudioClip[] swingTier1Clips;
    private AudioClip[] swingTier2Clips;
    private AudioSource voiceSource;

    // Track last played index to avoid repeats
    private int lastTier1Index = -1;
    private int lastTier2Index = -1;

    void Awake()
    {
        // Music source — plays one track at a time, picks next when done
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.loop = false;
        musicSource.volume = MUSIC_VOLUME;

        // SFX source — one-shots
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f;
        sfxSource.volume = 1.0f;

        // Tap source — dedicated so taps never compete with game over/dissolve sounds
        tapSource = gameObject.AddComponent<AudioSource>();
        tapSource.playOnAwake = false;
        tapSource.spatialBlend = 0f;
        tapSource.volume = 1.0f;

        // Rewind sound source — loops, pitch mapped to rewind speed
        rewindSource = gameObject.AddComponent<AudioSource>();
        rewindSource.playOnAwake = false;
        rewindSource.spatialBlend = 0f;
        rewindSource.loop = false;
        rewindSource.volume = REWIND_VOLUME;

        // Rolling sound source — loops, modulated by physics
        rollSource = gameObject.AddComponent<AudioSource>();
        rollSource.playOnAwake = false;
        rollSource.spatialBlend = 0f;
        rollSource.loop = true;
        rollSource.volume = 0f;

        // Voice line source — 2D, no spatial blend
        voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        voiceSource.spatialBlend = 0f;
        voiceSource.volume = 0.85f;

        LoadSwingClips();
        LoadSfx();
        LoadMusic();
    }

    void Start()
    {
        mTorus = FindAnyObjectByType<Torus>();
        mSphere = FindAnyObjectByType<Sphere>();
        mRewind = FindAnyObjectByType<RewindSystem>();

        GameObject ball = GameObject.Find("Ball");
        if (ball != null)
            mBallRb = ball.GetComponent<Rigidbody>();

        // Check if user is already playing music (Apple Music, Spotify, etc.)
        externalMusicPlaying = IsExternalMusicPlaying();

        // Restore user mute prefs
        LoadMutePrefs();

        // Start game music only if no external music is playing
        if (!externalMusicPlaying)
            PlayNextTrack();

        // Start rolling loop (silent until ball moves)
        if (rollClip != null)
        {
            rollSource.clip = rollClip;
            rollSource.Play();
        }
    }

    void LoadMusic()
    {
        musicClips = LoadNumberedClips("Audio/mainTrack", 20, startAt: 1);
        Debug.Log($"[GameAudio] Loaded {musicClips.Length} music track(s)");
    }

    void LoadSfx()
    {
        gameOverClip = Resources.Load<AudioClip>("Audio/sfx_gameover");
        rollClip = Resources.Load<AudioClip>("Audio/sfx_roll");
        rewindClip = Resources.Load<AudioClip>("Audio/sfx_rewind");
        glitchClip = Resources.Load<AudioClip>("Audio/sfx_glitch");
        tapClip = Resources.Load<AudioClip>("Audio/sfx_tap");
        countClip = Resources.Load<AudioClip>("Audio/sfx_count");
        gateClip = Resources.Load<AudioClip>("Audio/sfx_gate");
        newBestClip = Resources.Load<AudioClip>("Audio/sfx_newbest");
        top5Clip = Resources.Load<AudioClip>("Audio/sfx_top5");
        gateDissolveClip = Resources.Load<AudioClip>("Audio/sfx_gate_dissolve");
        gateSpawnClip = Resources.Load<AudioClip>("Audio/sfx_gate_spawn");
        Debug.Log($"[GameAudio] Loaded: gameOver={gameOverClip != null} roll={rollClip != null} tier1={swingTier1Clips.Length} tier2={swingTier2Clips.Length}");
    }

    void LoadSwingClips()
    {
        swingTier1Clips = LoadNumberedClips("Audio/swing_tier1_", 10);
        swingTier2Clips = LoadNumberedClips("Audio/swing_tier2_", 10);
    }

    AudioClip[] LoadNumberedClips(string prefix, int maxCount, int startAt = 0)
    {
        var clips = new System.Collections.Generic.List<AudioClip>();
        for (int i = startAt; i < startAt + maxCount; i++)
        {
            AudioClip clip = Resources.Load<AudioClip>(prefix + i);
            if (clip != null)
                clips.Add(clip);
            else
                break; // Stop at first gap
        }
        return clips.ToArray();
    }

    void Update()
    {
        if (mTorus == null) return;

        // Poll for gate pass (non-milestone only)
        if (mTorus.ConsumeGatePass())
            PlayGate();

        // Poll for swing events
        int tier = mTorus.ConsumeSwingEvent();
        if (tier > 0)
        {
            Debug.Log($"[GameAudio] Swing detected! Tier {tier}");
            PlaySwingVoice(tier);
        }

        UpdateRollingSound();
        UpdateRewindSound();

        // Auto-advance to next track when current ends (skip if external music is playing)
        if (!externalMusicPlaying && musicClips != null && musicClips.Length > 0 && !musicSource.isPlaying && musicSource.clip != null)
            PlayNextTrack();
    }

    void PlayNextTrack()
    {
        if (musicClips == null || musicClips.Length == 0) return;

        int index;
        if (musicClips.Length == 1)
        {
            index = 0;
        }
        else
        {
            do { index = Random.Range(0, musicClips.Length); }
            while (index == lastMusicIndex);
        }

        lastMusicIndex = index;
        musicSource.clip = musicClips[index];
        musicSource.Play();

        // Loop if only one track
        musicSource.loop = (musicClips.Length == 1);
    }

    void UpdateRewindSound()
    {
        if (rewindSource == null || rewindClip == null || mRewind == null) return;

        bool rewinding = mRewind.IsRewinding();

        if (rewinding && !rewindPlaying)
        {
            // Play once from the start, full volume
            rewindSource.clip = rewindClip;
            rewindSource.volume = REWIND_VOLUME;
            rewindSource.pitch = 1f;
            rewindSource.Play();
            rewindPlaying = true;
        }
        else if (rewinding && rewindPlaying)
        {
            // Fade out during last 20% of rewind
            float progress = mRewind.GetRewindProgress();
            if (progress > 0.8f)
            {
                float fadeT = (progress - 0.8f) / 0.2f; // 0..1 over last 20%
                rewindSource.volume = REWIND_VOLUME * (1f - fadeT);
            }
        }
        else if (!rewinding && rewindPlaying)
        {
            // Rewind ended — stop immediately
            rewindSource.Stop();
            rewindPlaying = false;
        }
    }

    void UpdateRollingSound()
    {
        if (rollSource == null || rollClip == null || mBallRb == null) return;

        // Silent when not playing (waiting, dead, rewinding)
        bool active = mSphere != null && !mSphere.IsWaiting() && !mSphere.IsGameOver() && !mSphere.IsRewinding();

        float targetVol = 0f;
        float targetPitch = ROLL_MIN_PITCH;

        if (active)
        {
            float vel = mBallRb.linearVelocity.magnitude;
            float t = Mathf.InverseLerp(ROLL_VEL_MIN, ROLL_VEL_MAX, vel);
            targetVol = Mathf.Lerp(ROLL_MIN_VOLUME, ROLL_MAX_VOLUME, t);
            targetPitch = Mathf.Lerp(ROLL_MIN_PITCH, ROLL_MAX_PITCH, t);
        }

        // Smooth volume to avoid jitter
        rollSource.volume = Mathf.SmoothDamp(rollSource.volume, targetVol, ref rollSmoothVel, ROLL_SMOOTH_TIME);
        rollSource.pitch = targetPitch;
    }

    public void PlayGameOver()
    {
        if (gameOverClip != null)
            sfxSource.PlayOneShot(gameOverClip);
    }

    public void PlayGlitch()
    {
        if (glitchClip != null)
            sfxSource.PlayOneShot(glitchClip);
    }

    public void PlayTap()
    {
        if (tapClip != null)
            tapSource.PlayOneShot(tapClip);
    }

    public void PlayCount()
    {
        if (countClip != null)
            sfxSource.PlayOneShot(countClip, 0.5f);
    }

    public void PlayGate()
    {
        if (gateClip != null)
            sfxSource.PlayOneShot(gateClip);
    }

    public void PlayNewBest(bool allTimeBest)
    {
        if (allTimeBest)
        {
            if (newBestClip != null)
                sfxSource.PlayOneShot(newBestClip);
        }
        else
        {
            if (top5Clip != null)
                sfxSource.PlayOneShot(top5Clip);
        }
    }

    public void PlayGateDissolve()
    {
        if (gateDissolveClip != null)
            sfxSource.PlayOneShot(gateDissolveClip);
    }

    public void PlayGateSpawn()
    {
        if (gateSpawnClip != null)
            sfxSource.PlayOneShot(gateSpawnClip);
    }

    void PlaySwingVoice(int tier)
    {
        // Don't overlap voice lines
        if (voiceSource.isPlaying) return;

        AudioClip[] clips;
        int lastIndex;

        if (tier >= 2 && swingTier2Clips.Length > 0)
        {
            clips = swingTier2Clips;
            lastIndex = lastTier2Index;
        }
        else if (swingTier1Clips.Length > 0)
        {
            clips = swingTier1Clips;
            lastIndex = lastTier1Index;
        }
        else
        {
            return; // No clips loaded
        }

        // Pick random clip, avoid repeating the same one
        int index;
        if (clips.Length == 1)
        {
            index = 0;
        }
        else
        {
            do { index = Random.Range(0, clips.Length); }
            while (index == lastIndex);
        }

        voiceSource.clip = clips[index];
        voiceSource.pitch = Random.Range(0.95f, 1.05f); // Subtle variation
        voiceSource.Play();

        if (tier >= 2)
            lastTier2Index = index;
        else
            lastTier1Index = index;
    }

    // ── PERSISTENT SETTINGS ─────────────────────────────────

    private const string PREF_MUSIC = "MuteMusic";
    private const string PREF_SOUND = "MuteSound";

    public void LoadMutePrefs()
    {
        bool muteM = PlayerPrefs.GetInt(PREF_MUSIC, 0) == 1;
        bool muteS = PlayerPrefs.GetInt(PREF_SOUND, 0) == 1;
        SetMusicMuted(muteM);
        SetSoundMuted(muteS);
    }

    public bool IsMusicMuted() { return musicSource != null && musicSource.mute; }
    public bool IsSoundMuted() { return sfxSource != null && sfxSource.mute; }

    public void SetMusicMuted(bool muted)
    {
        if (musicSource != null) musicSource.mute = muted;
        PlayerPrefs.SetInt(PREF_MUSIC, muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetSoundMuted(bool muted)
    {
        if (sfxSource != null) sfxSource.mute = muted;
        if (tapSource != null) tapSource.mute = muted;
        if (voiceSource != null) voiceSource.mute = muted;
        if (rollSource != null) rollSource.mute = muted;
        if (rewindSource != null) rewindSource.mute = muted;
        PlayerPrefs.SetInt(PREF_SOUND, muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ── DEBUG ────────────────────────────────────────────────

    private bool dbgMuteMusic, dbgMuteSfx, dbgMuteVoice, dbgMuteRoll, dbgMuteRewind;

    public void ApplyDebugMutes(bool music, bool sfx, bool voice, bool roll, bool rewind)
    {
        if (music != dbgMuteMusic)
        {
            dbgMuteMusic = music;
            if (musicSource != null) musicSource.mute = music;
        }
        if (sfx != dbgMuteSfx)
        {
            dbgMuteSfx = sfx;
            if (sfxSource != null) sfxSource.mute = sfx;
        }
        if (voice != dbgMuteVoice)
        {
            dbgMuteVoice = voice;
            if (voiceSource != null) voiceSource.mute = voice;
        }
        if (roll != dbgMuteRoll)
        {
            dbgMuteRoll = roll;
            if (rollSource != null) rollSource.mute = roll;
        }
        if (rewind != dbgMuteRewind)
        {
            dbgMuteRewind = rewind;
            if (rewindSource != null) rewindSource.mute = rewind;
        }
    }

    // ── EXTERNAL MUSIC DETECTION ────────────────────────────

    bool IsExternalMusicPlaying()
    {
#if UNITY_IOS && !UNITY_EDITOR
        return _IsOtherAudioPlaying();
#else
        return false;
#endif
    }
}
