using System;
using UnityEngine;

namespace MiceToBeHome
{
    public class LivesSystem : MonoBehaviour
    {
        public int Current { get; private set; }
        public int Max { get; private set; }

        public event Action<int, int> Changed;
        public event Action Depleted;

        private MousePlayerController player;
        private AudioManager audioManager;

        public void Initialize(MousePlayerController playerRef, int maxLives, AudioManager audioManager)
        {
            player = playerRef;
            Max = Mathf.Max(1, maxLives);
            this.audioManager = audioManager;

            if (player != null)
            {
                player.Hit += HandleHit;
            }

            ResetLives();
        }

        public void ResetLives()
        {
            Current = Max;
            Changed?.Invoke(Current, Max);
        }

        private void HandleHit()
        {
            if (Current <= 0)
            {
                return;
            }

            Current--;
            Changed?.Invoke(Current, Max);

            if (audioManager != null)
            {
                audioManager.PlayHit();
            }

            if (Current <= 0)
            {
                Depleted?.Invoke();
            }
        }

        private void OnDestroy()
        {
            if (player != null)
            {
                player.Hit -= HandleHit;
            }
        }
    }
}
