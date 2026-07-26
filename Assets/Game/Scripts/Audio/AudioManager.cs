using System.Collections.Generic;
using UnityEngine;

namespace MiceToBeHome
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private AudioBank bank;
        private AudioSource music;
        private AudioSource effects;
        private AudioSource mouseRunLoop;
        private AudioSource catPurrLoop;

        // Music sequencing: optional intro one-shots then a loop, on ONE shared source polled each
        // frame. WebGL-safe on purpose - no PlayScheduled/dspTime (those can fire every loop at once).
        private readonly List<AudioClip> introQueue = new List<AudioClip>();
        private AudioClip activeLoop;
        private bool playingIntro;
        private bool introStartedAudibly;
        private float introDeadline;
        private bool bootIntroPlayed;

        public float FootstepInterval => bank != null ? bank.footstepInterval : 0.28f;

        public void Initialize(AudioBank audioBank)
        {
            bank = audioBank;
            ApplyVolumes();
        }

        // Inspector-driven levels: music vs. all SFX (AudioBank.musicVolume / sfxVolume). Refreshed
        // every frame so the two sliders can be leveled live in Play mode.
        private void ApplyVolumes()
        {
            if (bank == null)
            {
                return;
            }
            if (music != null)
            {
                music.volume = bank.musicVolume;
            }
            if (effects != null)
            {
                effects.volume = bank.sfxVolume;
            }
            if (mouseRunLoop != null)
            {
                mouseRunLoop.volume = bank.sfxVolume;
            }
            if (catPurrLoop != null)
            {
                catPurrLoop.volume = bank.sfxVolume;
            }
        }

        private void Awake()
        {
            Instance = this;

            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.volume = 0.55f;

            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.volume = 0.8f;

            mouseRunLoop = gameObject.AddComponent<AudioSource>();
            mouseRunLoop.loop = true;
            mouseRunLoop.playOnAwake = false;
            mouseRunLoop.volume = 0.5f;

            catPurrLoop = gameObject.AddComponent<AudioSource>();
            catPurrLoop.loop = true;
            catPurrLoop.playOnAwake = false;
            catPurrLoop.volume = 0.6f;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void PlayMenuMusic()
        {
            if (bank == null)
            {
                return;
            }
            // The global intro plays only once, on the very first menu (game start).
            if (!bootIntroPlayed)
            {
                bootIntroPlayed = true;
                PlayMusicSequence(bank.menuLoop, bank.intro, bank.menuIntro);
            }
            else
            {
                PlayMusicSequence(bank.menuLoop, bank.menuIntro);
            }
        }

        public void PlayEditMusic() => PlayMusicSequence(bank?.placeLoop, bank?.placeIntro);
        public void PlayChaseMusic() => PlayMusicSequence(bank?.chaseLoop, bank?.chaseIntro);

        public void PlayHit() => PlayEffect(bank?.mouseHit);
        public void PlayVictory() => PlayEffect(bank?.victory);
        public void PlayDefeat() => PlayEffect(bank?.defeat);

        public void PlayHover() => PlayEffect(bank?.uiHover);
        public void PlayClick() => PlayEffect(bank?.uiClick);
        public void PlayMouseFootstep() => PlayEffect(bank?.mouseFootstep);
        public void PlayCatFootstep() => PlayEffect(bank?.catFootstep);
        public void PlayCatAttack() => PlayRandom(bank?.catAttack);
        public void PlayTrap(AudioClip clip) => PlayEffect(clip);

        public void PlayCatTrapped() => PlayRandom(bank?.catTrapped);

        private void PlayRandom(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return;
            }
            PlayEffect(clips[Random.Range(0, clips.Length)]);
        }

        public void SetMouseRunning(bool on) => SetLoop(mouseRunLoop, on ? bank?.mouseRun : null);
        public void SetCatPurring(bool on) => SetLoop(catPurrLoop, on ? bank?.catPurr : null);

        private static void SetLoop(AudioSource src, AudioClip clip)
        {
            if (src == null)
            {
                return;
            }
            if (clip == null)
            {
                if (src.isPlaying)
                {
                    src.Stop();
                }
                return;
            }
            if (src.clip == clip && src.isPlaying)
            {
                return;
            }
            src.clip = clip;
            src.Play();
        }

        // Plays optional one-shot intro clips in order, then loops `loop`. Null clips are skipped.
        private void PlayMusicSequence(AudioClip loop, params AudioClip[] intros)
        {
            if (music == null)
            {
                return;
            }

            // Already on this exact theme - let it keep playing instead of restarting it.
            if (activeLoop == loop && (playingIntro || music.isPlaying))
            {
                return;
            }

            activeLoop = loop;
            introQueue.Clear();
            if (intros != null)
            {
                for (int i = 0; i < intros.Length; i++)
                {
                    if (intros[i] != null)
                    {
                        introQueue.Add(intros[i]);
                    }
                }
            }

            AdvanceMusic();
        }

        private void Update()
        {
            ApplyVolumes();

            if (!playingIntro || music == null)
            {
                return;
            }

            if (music.isPlaying)
            {
                introStartedAudibly = true;
            }

            // Advance once the intro has actually played out (or a safety deadline passes, in case a
            // browser keeps isPlaying stuck). One source polled one clip at a time can never overlap loops.
            if ((introStartedAudibly && !music.isPlaying) || Time.unscaledTime >= introDeadline)
            {
                AdvanceMusic();
            }
        }

        private void AdvanceMusic()
        {
            if (introQueue.Count > 0)
            {
                AudioClip next = introQueue[0];
                introQueue.RemoveAt(0);
                music.loop = false;
                music.clip = next;
                music.Play();
                playingIntro = true;
                introStartedAudibly = false;
                introDeadline = Time.unscaledTime + Mathf.Max(0.1f, next.length) + 0.5f;
                return;
            }

            playingIntro = false;
            if (activeLoop == null)
            {
                music.Stop();
                return;
            }

            music.loop = true;
            music.clip = activeLoop;
            music.Play();
        }

        private void PlayEffect(AudioClip clip)
        {
            if (clip != null && effects != null)
            {
                effects.PlayOneShot(clip);
            }
        }
    }
}
