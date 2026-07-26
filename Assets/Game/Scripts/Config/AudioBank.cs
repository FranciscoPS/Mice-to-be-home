using System;
using UnityEngine;

namespace MiceToBeHome
{
    [Serializable]
    public class AudioBank
    {
        [Header("Volume")]
        [Range(0f, 1f)] public float musicVolume = 0.6f;
        [Range(0f, 1f)] public float sfxVolume = 0.8f;

        [Header("Music (optional) - 'intro' clips play once, then the matching loop repeats")]
        [Tooltip("Global startup sting. Plays once when the game first opens, before the menu music.")]
        public AudioClip intro;
        [Tooltip("Menu: plays once when entering the menu, before the menu loop.")]
        public AudioClip menuIntro;
        [Tooltip("Menu: loops while sitting in the menu.")]
        public AudioClip menuLoop;
        [Tooltip("Placement: plays once when trap placement starts, before the placement loop.")]
        public AudioClip placeIntro;
        [Tooltip("Placement: loops while placing traps (the chill section).")]
        public AudioClip placeLoop;
        [Tooltip("Chase: the bridge/transition, plays once when the chase starts, before the chase loop.")]
        public AudioClip chaseIntro;
        [Tooltip("Chase: loops during the cat chase (the rock section).")]
        public AudioClip chaseLoop;

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
