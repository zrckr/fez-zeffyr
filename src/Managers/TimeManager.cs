using Godot;
using System;
using Zeffyr.Structure;
using Zeffyr.Tools;

namespace Zeffyr.Managers
{
    public class TimeManager : Node
    {
        private const float TransitionDivider = 3f;
        private const float DefaultTimeFactor = 260f;
        
        private static DateTime Initial = DateTime.Today.AddHours(12.0);
        
        private float _sinceStart;
        private Func<float, bool> _onAct;

        [Signal] public delegate void Tick();
        
        public DateTime CurrentTime { get; set; }

        public float TimeFactor { get; set; }

        public float DayFraction => (float) CurrentTime.TimeOfDay.TotalDays;

        public float DayTransition { get; private set; }
        
        public float NightTransition { get; private set; }

        public float DawnTransition { get; private set; }

        public float DuskTransition { get; private set; }
        
        public int Hour
        {
            get => CurrentTime.TimeOfDay.Hours;
            set => CurrentTime = CurrentTime.Date.AddHours(value);
        }

        public float NextHourContribution
        {
            get
            {
                TimeSpan current = CurrentTime.TimeOfDay;
                TimeSpan next = TimeSpan.FromHours(current.Hours + 1);
                return (float)(1d - (next - current).TotalHours);
            }
        }

        public bool IsDayPhase(DayPhase phase)
        {
            float dayFraction = DayFraction;
            float start = phase.Start;
            float end = phase.End;
            return start < end
                ? dayFraction >= start && dayFraction <= end
                : dayFraction >= start || dayFraction <= end;
        }

        public bool IsDayPhaseForMusic(DayPhase phase)
        {
            float dayFraction = DayFraction;
            float start = phase.MusicStart;
            float end = phase.MusicEnd;
            return start < end
                ? dayFraction >= start && dayFraction <= end
                : dayFraction >= start || dayFraction <= end;
        }

        public void SetHourSmooth(int hour)
        {
            DateTime next = CurrentTime.Date.AddHours(hour);
            int direction = Mathf.Sign(next.Ticks - CurrentTime.Ticks);

            _onAct += (total) =>
            {
                bool reached = direction != Mathf.Sign(next.Ticks - CurrentTime.Ticks);
                if (!reached && total < 1f)
                {
                    float factor = Mathz.Clamp01(total);
                    TimeFactor = DefaultTimeFactor * Easing.In.Quad(factor) * 100f * direction;
                }
                else if (reached)
                {
                    TimeFactor = DefaultTimeFactor;
                }
                return reached;
            };
        }

        public void IncrementTimeFactor(float secondsUntilDouble)
        {
            float diff = secondsUntilDouble / GetProcessDeltaTime();
            float factor = Mathf.IsZeroApprox(secondsUntilDouble) ? 0f : Mathf.Pow(2f, 1f / diff);
            TimeFactor *= factor;
        }

        public override void _Ready()
        {
            this.InjectNodes();
            PauseMode = PauseModeEnum.Stop;
            TimeFactor = DefaultTimeFactor;
            CurrentTime = TimeManager.Initial;
            SetProcess(false);
        }

        public override void _Process(float delta)
        {
            DateTime prevTick = CurrentTime;
            CurrentTime += TimeSpan.FromSeconds(delta * TimeFactor);

            if (Mathf.Abs(CurrentTime.TimeOfDay.Minutes - prevTick.TimeOfDay.Minutes) >= 1)
                EmitSignal(nameof(Tick));
            
            DawnTransition = Ease(DayFraction, DayPhase.Dawn.Start, DayPhase.Dawn.Duration);
            DuskTransition = Ease(DayFraction, DayPhase.Dusk.Start, DayPhase.Dusk.Duration);
            
            DayTransition = Ease(DayFraction, DayPhase.Day.Start, DayPhase.Day.Duration);
            DayTransition = Mathf.Max(DayTransition, 
                Ease(DayFraction, DayPhase.Day.Start - 1f, DayPhase.Day.Duration));
            
            NightTransition = Ease(DayFraction, DayPhase.Night.Start, DayPhase.Night.Duration);
            NightTransition = Mathf.Max(NightTransition, 
                Ease(DayFraction, DayPhase.Night.Start - 1f, DayPhase.Night.Duration));

            if (_onAct is not null)
            {
                _sinceStart += delta;
                if (_onAct.Invoke(_sinceStart))
                {
                    _onAct = null;
                    _sinceStart = 0f;
                }
            }
        }

        private static float Ease(float value, float start, float duration)
        {
            float diff = value - start;
            float step = duration / TransitionDivider;
            
            if (diff < step)
                return Mathz.Clamp01(diff / step);
            if (diff > step * 2f)
                return 1f - Mathz.Clamp01((diff - step * 2f) / step);
            return diff < 0f || diff > duration ? 0f : 1f;
        }

        public void Reset()
        {
            SetProcess(true);
            CurrentTime = TimeManager.Initial;
            TimeFactor = DefaultTimeFactor;
        }
    }
}