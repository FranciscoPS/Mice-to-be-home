using UnityEngine;

namespace MiceToBeHome
{
    public class AudioManager : MonoBehaviour
    {
        private AudioBank bank;
        private AudioSource music;
        private AudioSource effects;

        public void Initialize(AudioBank audioBank)
        {
            bank = audioBank;
        }

        private void Awake()
        {
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.volume = 0.55f;

            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.volume = 0.8f;
        }

        public void PlayMenuMusic() => PlayMusic(bank?.menuMusic);
        public void PlayEditMusic() => PlayMusic(bank?.editMusic);
        public void PlayChaseMusic() => PlayMusic(bank?.chaseMusic);

        public void PlayPlace() => PlayEffect(bank?.placeTrap);
        public void PlayRotate() => PlayEffect(bank?.rotateTrap);
        public void PlayDistract() => PlayEffect(bank?.catDistracted);
        public void PlayHit() => PlayEffect(bank?.mouseHit);
        public void PlayVictory() => PlayEffect(bank?.victory);
        public void PlayDefeat() => PlayEffect(bank?.defeat);

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
