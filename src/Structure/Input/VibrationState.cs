using System;
using Zeffyr.Tools;

namespace Zeffyr.Structure.Input
{
    public struct VibrationState
    {
        public bool Active { get; private set; }

        public float Elapsed { get; private set; }

        public float MaxAmount { get; private set; }

        public float Duration { get; private set; }

        public Func<float, float> Easing { get; private set; }

        public float LastAmount { get; private set; }

        private float _currentAmount;

        public float CurrentAmount
        {
            get => _currentAmount;
            set
            {
                LastAmount = _currentAmount;
                _currentAmount = value;
            }
        }

        public VibrationState(float maxAmount, float duration, Func<float, float> easing) : this()
        {
            Active = true;
            LastAmount = CurrentAmount = 0.0f;
            Elapsed = 0f;
            MaxAmount = Mathz.Clamp01(maxAmount);
            Duration = duration;
            Easing = easing;
        }

        public void Update(float delta)
        {
            if (Elapsed <= Duration)
            {
                float factor = Easing(1f - Elapsed / Duration);
                CurrentAmount = factor * MaxAmount;
            }
            else
            {
                CurrentAmount = 0f;
                Active = false;
            }

            Elapsed += delta;
        }
    }
}