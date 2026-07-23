using System;

namespace MiceToBeHome
{
    public class Countdown
    {
        public float Remaining { get; private set; }
        public float Duration { get; private set; }
        public bool Running { get; private set; }

        public event Action<float> Changed;
        public event Action Finished;

        public void Begin(float duration)
        {
            Duration = duration;
            Remaining = duration;
            Running = true;
            Changed?.Invoke(Remaining);
        }

        public void Stop()
        {
            Running = false;
        }

        public void Tick(float deltaTime)
        {
            if (!Running)
            {
                return;
            }

            Remaining -= deltaTime;
            if (Remaining <= 0f)
            {
                Remaining = 0f;
                Running = false;
                Changed?.Invoke(Remaining);
                Finished?.Invoke();
                return;
            }

            Changed?.Invoke(Remaining);
        }
    }
}
