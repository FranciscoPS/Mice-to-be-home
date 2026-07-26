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

        public float FootstepInterval => bank != null ? bank.footstepInterval : 0.28f;

        public void Initialize(AudioBank audioBank)
        {
            bank = audioBank;
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

        public void PlayMenuMusic() => PlayMusic(bank?.menuMusic);
        public void PlayEditMusic() => PlayMusic(bank?.editMusic);
        public void PlayChaseMusic() => PlayMusic(bank?.chaseMusic);

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

        private void PlayMusic(AudioClip clip)
        {
            if (music == null)
            {
                return;
            }

            if (clip == null)
            {
                music.Stop();
                return;
            }

            if (music.clip == clip && music.isPlaying)
            {
                return;
            }

            music.clip = clip;
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
