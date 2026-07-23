using System;
using UnityEngine;

namespace MiceToBeHome
{
    [Serializable]
    public class AudioBank
    {
        [Header("Musica (opcional)")]
        public AudioClip menuMusic;
        public AudioClip editMusic;
        public AudioClip chaseMusic;

        [Header("Efectos (opcional)")]
        public AudioClip placeTrap;
        public AudioClip rotateTrap;
        public AudioClip catDistracted;
        public AudioClip mouseHit;
        public AudioClip victory;
        public AudioClip defeat;
    }
}
