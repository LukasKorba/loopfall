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
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern bool _IsOtherAudioPlaying();
#endif

    private Torus mTorus;
    private Rigidbody mBallRb;
    private Sphere mSphere;
    private RewindSystem mRewind;

    // ── MUSIC (double-buffered crossfade) ─────────────────
    private bool externalMusicPlaying = false;
    private AudioSource musicSourceA;
    private AudioSource musicSourceB;
    private AudioSource activeMusicSource;   // Currently audible
    private AudioSource standbyMusicSource;  // Pre-loaded, waiting
    private AudioClip[] musicClips;
    private int lastMusicIndex = -1;
    private const float MUSIC_VOLUME = 0.2f;
    private const float CROSSFADE_DURATION = 2.5f;
    private const float CROSSFADE_LEAD = 3.5f; // Start crossfade this many seconds before track ends
    private bool crossfading = false;
    private float crossfadeTimer = 0f;
    private bool standbyReady = false;
    // Post-swap, defer the next-track preload. The hitch we saw in capture was
    // Unity kicking off lazy decompression on the same frame the swap completed,
    // landing the cost inside active gameplay. 5s pushes it clear of the crossfade
    // and the player's immediate reactions; next track is still ready minutes early.
    private const float STANDBY_LOAD_DELAY = 5f;
    private float standbyLoadTimer = -1f; // -1 = inactive; >=0 = counting down

    // ── SFX ──────────────────────────────────────────────────
    private AudioSource sfxSource;
    private AudioSource tapSource; // Dedicated so taps never get dropped
    private AudioClip[] gameOverClips;
    private AudioClip glitchClip;
    private AudioClip[] tapClips;
    private AudioClip countClip;
    private AudioClip gateClip;
    private AudioClip newBestClip;
    private AudioClip top5Clip;
    private AudioClip gateDissolveClip;
    private AudioClip gateSpawnClip;

    // ── REWIND SOUND ─────────────────────────────────────────
    private AudioSource rewindSource;
    private AudioClip[] rewindClips;
    private bool rewindPlaying = false;
    private float rewindFadeVel = 0f;
    private const float REWIND_VOLUME = 0.5f;
    private const float REWIND_FADE_OUT = 0.3f; // Fade duration in seconds

    // ── ROLLING SOUND ────────────────────────────────────────
    private AudioSource rollSource;
    private AudioClip[] rollClips;
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

    // ── BLITZ SFX ────────────────────────────────────────────
    private AudioClip[] beamFireClips;
    private AudioClip beamFireHotClip;
    private AudioClip blitzDeathClip;
    private AudioClip boxHitClip;
    private AudioClip buttonDestroyedClip;
    private AudioClip gatePassFullClip;
    private AudioClip gatePassHalfClip;
    private AudioClip orbPickupCadencyClip;
    private AudioClip orbPickupGunClip;
    private AudioClip orbPickupShieldClip;
    private AudioClip sentinelHitClip;
    private AudioClip sentinelKillClip;
    private AudioClip shieldAbsorbClip;
    private AudioClip[] strandPeelClips; // indexed by peel order (0=first, 1=second, 2=destroy)
    private AudioClip wipeOutClip;
    private AudioClip wipeInClip;

    // Blitz upgrade voice lines
    private AudioClip voiceCannonUpClip;
    private AudioClip voiceCannonFullClip;
    private AudioClip voiceCadenceUpClip;
    private AudioClip voiceCadenceFullClip;
    private AudioClip voiceShieldOnClip;

    void Awake()
    {
        // Music sources — double-buffered for lag-free crossfade
        musicSourceA = gameObject.AddComponent<AudioSource>();
        musicSourceA.playOnAwake = false;
        musicSourceA.spatialBlend = 0f;
        musicSourceA.loop = false;
        musicSourceA.volume = MUSIC_VOLUME;

        musicSourceB = gameObject.AddComponent<AudioSource>();
        musicSourceB.playOnAwake = false;
        musicSourceB.spatialBlend = 0f;
        musicSourceB.loop = false;
        musicSourceB.volume = 0f;

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
        LoadBlitzSfx();
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

        // Start rolling loop (silent until ball moves) — pick a random variant
        AudioClip roll = PickRandom(rollClips);
        if (roll != null)
        {
            rollSource.clip = roll;
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
        gameOverClips = LoadVariants("Audio/sfx_gameover");
        rollClips = LoadVariants("Audio/sfx_roll");
        rewindClips = LoadVariants("Audio/sfx_rewind");
        tapClips = LoadVariants("Audio/sfx_tap");
        glitchClip = Resources.Load<AudioClip>("Audio/sfx_glitch");
        countClip = Resources.Load<AudioClip>("Audio/sfx_count");
        gateClip = Resources.Load<AudioClip>("Audio/sfx_gate");
        newBestClip = Resources.Load<AudioClip>("Audio/sfx_newbest");
        top5Clip = Resources.Load<AudioClip>("Audio/sfx_top5");
        gateDissolveClip = Resources.Load<AudioClip>("Audio/sfx_gate_dissolve");
        gateSpawnClip = Resources.Load<AudioClip>("Audio/sfx_gate_spawn");
        Debug.Log($"[GameAudio] Loaded: gameOver={gameOverClips.Length} roll={rollClips.Length} rewind={rewindClips.Length} tap={tapClips.Length} tier1={swingTier1Clips.Length} tier2={swingTier2Clips.Length}");
    }

    void LoadBlitzSfx()
    {
        beamFireClips = LoadVariants("Audio/Blitz/sfx_beam_fire1", "Audio/Blitz/sfx_beam_fire2", "Audio/Blitz/sfx_beam_fire3");
        beamFireHotClip      = Resources.Load<AudioClip>("Audio/Blitz/sfx_beam_fire_hot");
        blitzDeathClip       = Resources.Load<AudioClip>("Audio/Blitz/sfx_blitz_death");
        boxHitClip           = Resources.Load<AudioClip>("Audio/Blitz/sfx_box_hit");
        buttonDestroyedClip  = Resources.Load<AudioClip>("Audio/Blitz/sfx_button_destroyed");
        gatePassFullClip     = Resources.Load<AudioClip>("Audio/Blitz/sfx_gate_pass_full");
        gatePassHalfClip     = Resources.Load<AudioClip>("Audio/Blitz/sfx_gate_pass_half");
        orbPickupCadencyClip = Resources.Load<AudioClip>("Audio/Blitz/sfx_orb_pickup_cadency");
        orbPickupGunClip     = Resources.Load<AudioClip>("Audio/Blitz/sfx_orb_pickup_gun");
        orbPickupShieldClip  = Resources.Load<AudioClip>("Audio/Blitz/sfx_orb_pickup_shield");
        sentinelHitClip      = Resources.Load<AudioClip>("Audio/Blitz/sfx_sentinel_hit");
        sentinelKillClip     = Resources.Load<AudioClip>("Audio/Blitz/sfx_sentinel_kill");
        shieldAbsorbClip     = Resources.Load<AudioClip>("Audio/Blitz/sfx_shield_absorb");
        strandPeelClips      = LoadVariants("Audio/Blitz/sfx_strand_peel_v1", "Audio/Blitz/sfx_strand_peel_v2", "Audio/Blitz/sfx_strand_peel_v3");
        wipeOutClip          = Resources.Load<AudioClip>("Audio/Blitz/sfx_blitz_wipe_out");
        wipeInClip           = Resources.Load<AudioClip>("Audio/Blitz/sfx_blitz_wipe_in");

        voiceCannonUpClip    = Resources.Load<AudioClip>("Audio/Blitz/voice_cannon_up_0");
        voiceCannonFullClip  = Resources.Load<AudioClip>("Audio/Blitz/voice_cannon_full_0");
        voiceCadenceUpClip   = Resources.Load<AudioClip>("Audio/Blitz/voice_cadence_up_0");
        voiceCadenceFullClip = Resources.Load<AudioClip>("Audio/Blitz/voice_cadence_full_0");
        voiceShieldOnClip    = Resources.Load<AudioClip>("Audio/Blitz/voice_shield_on_0");
    }

    AudioClip[] LoadVariants(params string[] paths)
    {
        var clips = new System.Collections.Generic.List<AudioClip>();
        for (int i = 0; i < paths.Length; i++)
        {
            AudioClip clip = Resources.Load<AudioClip>(paths[i]);
            if (clip != null) clips.Add(clip);
        }
        return clips.ToArray();
    }

    AudioClip PickRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return null;
        return clips[Random.Range(0, clips.Length)];
    }

    void LoadSwingClips()
    {
        swingTier1Clips = LoadNumberedClips("Audio/swing_tier1_", 10);
        swingTier2Clips = LoadNumberedClips("Audio/swing_tier2_", 20);
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
        UpdateMusicCrossfade();
    }

    void PlayNextTrack()
    {
        if (musicClips == null || musicClips.Length == 0) return;

        int index = PickNextMusicIndex();
        lastMusicIndex = index;

        if (activeMusicSource == null)
        {
            // First play — bootstrap A as active, B as standby
            activeMusicSource = musicSourceA;
            standbyMusicSource = musicSourceB;
        }

        activeMusicSource.clip = musicClips[index];
        activeMusicSource.volume = MUSIC_VOLUME;
        activeMusicSource.loop = (musicClips.Length == 1); // Loop only if single track
        activeMusicSource.Play();

        crossfading = false;
        standbyReady = false;

        // Pre-load the next track onto standby immediately
        if (musicClips.Length > 1)
            PrepareStandby();
    }

    int PickNextMusicIndex()
    {
        if (musicClips.Length == 1) return 0;
        int index;
        do { index = Random.Range(0, musicClips.Length); }
        while (index == lastMusicIndex);
        return index;
    }

    void PrepareStandby()
    {
        if (musicClips == null || musicClips.Length <= 1) { standbyReady = false; return; }

        int nextIndex = PickNextMusicIndex();
        AudioClip clip = musicClips[nextIndex];
        standbyMusicSource.clip = clip;
        standbyMusicSource.volume = 0f;

        // Pre-warm: kick Unity's audio thread to decompress now rather than at
        // the next Play(). LoadAudioData is async and returns immediately; work
        // happens on the audio/IO thread, not the main thread — exactly the
        // parallelism we want.
        if (clip != null && clip.loadState != AudioDataLoadState.Loaded)
            clip.LoadAudioData();

        standbyReady = true;
        standbyLoadTimer = -1f;
    }

    void UpdateMusicCrossfade()
    {
        // Deferred standby-load tick. Runs before the early-returns so the
        // timer can't stall if the active source hiccups mid-play.
        if (standbyLoadTimer >= 0f)
        {
            standbyLoadTimer -= Time.unscaledDeltaTime;
            if (standbyLoadTimer <= 0f) PrepareStandby();
        }

        if (externalMusicPlaying || activeMusicSource == null || !activeMusicSource.isPlaying) return;
        if (musicClips == null || musicClips.Length <= 1) return;

        AudioClip activeClip = activeMusicSource.clip;
        if (activeClip == null) return;

        float remaining = activeClip.length - activeMusicSource.time;

        // Start crossfade when approaching end
        if (!crossfading && standbyReady && remaining <= CROSSFADE_LEAD)
        {
            crossfading = true;
            crossfadeTimer = 0f;

            // Remember which index we're about to play
            lastMusicIndex = System.Array.IndexOf(musicClips, standbyMusicSource.clip);

            standbyMusicSource.volume = 0f;
            standbyMusicSource.Play();
        }

        if (crossfading)
        {
            crossfadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(crossfadeTimer / CROSSFADE_DURATION);
            float eased = t * t * (3f - 2f * t); // smoothstep

            activeMusicSource.volume = MUSIC_VOLUME * (1f - eased);
            standbyMusicSource.volume = MUSIC_VOLUME * eased;

            if (t >= 1f)
            {
                // Swap roles
                activeMusicSource.Stop();
                AudioSource temp = activeMusicSource;
                activeMusicSource = standbyMusicSource;
                standbyMusicSource = temp;

                crossfading = false;
                standbyReady = false;

                // Defer the next-track preload — tick runs below in Update().
                standbyLoadTimer = STANDBY_LOAD_DELAY;
            }
        }
    }

    void UpdateRewindSound()
    {
        if (rewindSource == null || rewindClips == null || rewindClips.Length == 0 || mRewind == null) return;

        bool rewinding = mRewind.IsRewinding();

        if (rewinding && !rewindPlaying)
        {
            // Play once from the start, full volume — pick a random variant
            rewindSource.clip = PickRandom(rewindClips);
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
        if (rollSource == null || rollClips == null || rollClips.Length == 0 || mBallRb == null) return;

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
        AudioClip clip = PickRandom(gameOverClips);
        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }

    public void PlayGlitch()
    {
        if (glitchClip != null)
            sfxSource.PlayOneShot(glitchClip);
    }

    public void PlayTap()
    {
        AudioClip clip = PickRandom(tapClips);
        if (clip != null)
            tapSource.PlayOneShot(clip);
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

    // ── BLITZ PLAYBACK ───────────────────────────────────────

    public void PlayBeamFire(int gunLevel)
    {
        // L2 swaps to the hot variant; L0/L1 rotate through beam_fire1/2/3.
        AudioClip clip = (gunLevel >= 2 && beamFireHotClip != null)
            ? beamFireHotClip
            : PickRandom(beamFireClips);
        if (clip != null) sfxSource.PlayOneShot(clip, 0.55f);
    }

    public void PlayBoxHit()
    {
        if (boxHitClip != null) sfxSource.PlayOneShot(boxHitClip);
    }

    public void PlaySentinelHit()
    {
        if (sentinelHitClip != null) sfxSource.PlayOneShot(sentinelHitClip);
    }

    public void PlaySentinelKill()
    {
        if (sentinelKillClip != null) sfxSource.PlayOneShot(sentinelKillClip);
    }

    public void PlayStrandPeel(int peelIndex)
    {
        if (strandPeelClips == null || strandPeelClips.Length == 0) return;
        int idx = Mathf.Clamp(peelIndex, 0, strandPeelClips.Length - 1);
        sfxSource.PlayOneShot(strandPeelClips[idx]);
    }

    public void PlayButtonDestroyed()
    {
        if (buttonDestroyedClip != null) sfxSource.PlayOneShot(buttonDestroyedClip);
    }

    public void PlayBlitzGatePassHalf()
    {
        if (gatePassHalfClip != null) sfxSource.PlayOneShot(gatePassHalfClip);
    }

    public void PlayBlitzGatePassFull()
    {
        if (gatePassFullClip != null) sfxSource.PlayOneShot(gatePassFullClip);
    }

    public void PlayOrbGun()     { if (orbPickupGunClip != null)     sfxSource.PlayOneShot(orbPickupGunClip); }
    public void PlayOrbCadency() { if (orbPickupCadencyClip != null) sfxSource.PlayOneShot(orbPickupCadencyClip); }
    public void PlayOrbShield()  { if (orbPickupShieldClip != null)  sfxSource.PlayOneShot(orbPickupShieldClip); }

    public void PlayShieldAbsorb()
    {
        if (shieldAbsorbClip != null) sfxSource.PlayOneShot(shieldAbsorbClip);
    }

    public void PlayBlitzDeath()
    {
        if (blitzDeathClip != null) sfxSource.PlayOneShot(blitzDeathClip);
    }

    public void PlayBlitzWipeOut()
    {
        if (wipeOutClip != null) sfxSource.PlayOneShot(wipeOutClip);
    }

    public void PlayBlitzWipeIn()
    {
        if (wipeInClip != null) sfxSource.PlayOneShot(wipeInClip);
    }

    public void PlayVoiceCannonUp()    { PlayBlitzVoice(voiceCannonUpClip); }
    public void PlayVoiceCannonFull()  { PlayBlitzVoice(voiceCannonFullClip); }
    public void PlayVoiceCadenceUp()   { PlayBlitzVoice(voiceCadenceUpClip); }
    public void PlayVoiceCadenceFull() { PlayBlitzVoice(voiceCadenceFullClip); }
    public void PlayVoiceShieldOn()    { PlayBlitzVoice(voiceShieldOnClip); }

    void PlayBlitzVoice(AudioClip clip)
    {
        if (clip == null) return;
        if (voiceSource.isPlaying) return; // mirror swing-voice policy: no overlaps
        voiceSource.clip = clip;
        voiceSource.pitch = 1f;
        voiceSource.Play();
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

    public bool IsMusicMuted() { return musicSourceA != null && musicSourceA.mute; }
    public bool IsSoundMuted() { return sfxSource != null && sfxSource.mute; }

    public void SetMusicMuted(bool muted)
    {
        if (musicSourceA != null) musicSourceA.mute = muted;
        if (musicSourceB != null) musicSourceB.mute = muted;
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
            if (musicSourceA != null) musicSourceA.mute = music;
            if (musicSourceB != null) musicSourceB.mute = music;
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
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
        return _IsOtherAudioPlaying();
#else
        return false;
#endif
    }
}
