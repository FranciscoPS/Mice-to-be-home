using System;
using UnityEngine;

namespace MiceToBeHome
{
    [Serializable]
    public class AudioBank
    {
        [Header("Music (optional)")]
        public AudioClip menuMusic;
        public AudioClip editMusic;
        public AudioClip chaseMusic;

        [Header("SFX (optional)")]
        public AudioClip mouseHit;
        public AudioClip victory;
        public AudioClip defeat;

        [Header("UI SFX")]
        public AudioClip uiHover;
        public AudioClip uiClick;

        [Header("Mouse SFX")]
        [Tooltip("Looped while the mouse is moving.")]
        public AudioClip mouseRun;
        [Tooltip("Ticked repeatedly while the mouse is moving.")]
        public AudioClip mouseFootstep;

        [Header("Cat SFX")]
        [Tooltip("Ticked repeatedly while the cat is moving.")]
        public AudioClip catFootstep;
        [Tooltip("Looped while the cat is stunned / distracted.")]
        public AudioClip catPurr;
        [Tooltip("One is picked at random each time the cat attacks the player (varies to avoid repetition).")]
        public AudioClip[] catAttack = new AudioClip[0];
        [Tooltip("One is picked at random when the cat hits a trap (varies to avoid repetition).")]
        public AudioClip[] catTrapped = new AudioClip[0];

        [Header("Movement")]
        [Min(0.05f)]
        [Tooltip("Seconds between footstep ticks while moving.")]
        public float footstepInterval = 0.28f;
    }
}
