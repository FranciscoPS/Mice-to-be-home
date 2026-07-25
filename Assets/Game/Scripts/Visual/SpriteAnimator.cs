using System;
using UnityEngine;

namespace MiceToBeHome
{
    public enum CharacterAnim
    {
        Idle,
        Run,
        Transform
    }

    /// <summary>
    /// Data-driven flipbook animator. Drives its own <see cref="SpriteRenderer"/> from a set of
    /// Inspector-configured clips (frames + speed + loop). Reused by the player and the cat.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteAnimator : MonoBehaviour
    {
        [Serializable]
        public class Clip
        {
            public CharacterAnim state = CharacterAnim.Idle;

            [Tooltip("Frames for this animation, in order.")]
            public Sprite[] frames = new Sprite[0];

            [Min(0.1f)]
            [Tooltip("Playback speed in frames per second.")]
            public float framesPerSecond = 10f;

            [Tooltip("Loop forever (Idle / Run) or play once (Transform).")]
            public bool loop = true;
        }

        [Tooltip("One entry per animation state. Drag the sliced frames into each clip.")]
        [SerializeField] private Clip[] clips = new Clip[0];

        private SpriteRenderer spriteRenderer;
        private Clip current;
        private int frameIndex;
        private float frameTimer;
        private bool playing;
        private Action onComplete;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>True when a clip for this state exists and actually has frames assigned.</summary>
        public bool Has(CharacterAnim state)
        {
            Clip clip = FindClip(state);
            return clip != null && clip.frames != null && clip.frames.Length > 0;
        }

        /// <summary>
        /// Plays the clip for <paramref name="state"/>. Looping clips ignore repeated calls so
        /// controllers can call this every frame. One-shot clips invoke <paramref name="onDone"/>
        /// when the last frame is reached. Missing/empty clips keep the current sprite and finish
        /// immediately.
        /// </summary>
        public void Play(CharacterAnim state, Action onDone = null)
        {
            Clip clip = FindClip(state);

            if (clip != null && clip == current && clip.loop && playing)
            {
                return;
            }

            onComplete = onDone;
            current = clip;
            frameIndex = 0;
            frameTimer = 0f;

            if (clip == null || clip.frames == null || clip.frames.Length == 0)
            {
                playing = false;
                InvokeComplete();
                return;
            }

            playing = true;
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = clip.frames[0];
            }
        }

        /// <summary>Immediately finishes the current one-shot clip (used to skip the intro).</summary>
        public void Complete()
        {
            if (!playing || current == null || current.loop)
            {
                return;
            }

            frameIndex = current.frames.Length - 1;
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = current.frames[frameIndex];
            }
            playing = false;
            InvokeComplete();
        }

        private void Update()
        {
            if (!playing || current == null)
            {
                return;
            }

            float frameDuration = 1f / Mathf.Max(0.1f, current.framesPerSecond);
            frameTimer += Time.deltaTime;
            while (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                frameIndex++;
                if (frameIndex >= current.frames.Length)
                {
                    if (current.loop)
                    {
                        frameIndex = 0;
                    }
                    else
                    {
                        frameIndex = current.frames.Length - 1;
                        if (spriteRenderer != null)
                        {
                            spriteRenderer.sprite = current.frames[frameIndex];
                        }
                        playing = false;
                        InvokeComplete();
                        return;
                    }
                }

                if (spriteRenderer != null)
                {
                    spriteRenderer.sprite = current.frames[frameIndex];
                }
            }
        }

        private Clip FindClip(CharacterAnim state)
        {
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null && clips[i].state == state)
                {
                    return clips[i];
                }
            }
            return null;
        }

        private void InvokeComplete()
        {
            Action callback = onComplete;
            onComplete = null;
            callback?.Invoke();
        }

#if UNITY_EDITOR
        /// <summary>Editor-only: pre-creates empty, labelled clip slots when Build Scene makes the prefab.</summary>
        public void EditorSeedClips(CharacterAnim[] states)
        {
            clips = new Clip[states.Length];
            for (int i = 0; i < states.Length; i++)
            {
                clips[i] = new Clip
                {
                    state = states[i],
                    frames = new Sprite[0],
                    framesPerSecond = 10f,
                    loop = states[i] != CharacterAnim.Transform
                };
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            var renderer = GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                return;
            }

            Clip idle = FindClip(CharacterAnim.Idle);
            if (idle != null && idle.frames != null && idle.frames.Length > 0 && idle.frames[0] != null)
            {
                renderer.sprite = idle.frames[0];
            }
        }
#endif
    }
}
