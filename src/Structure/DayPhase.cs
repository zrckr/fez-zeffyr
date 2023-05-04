namespace Zeffyr.Structure
{
    public record DayPhase
    {
        public static DayPhase Night = new DayPhase(20, 04, 21, 02);
        public static DayPhase Dawn = new DayPhase(02, 06, 02, 05);
        public static DayPhase Day = new DayPhase(05, 20, 05, 19);
        public static DayPhase Dusk = new DayPhase(18, 22, 19, 21);

        public float Start;

        public float End;

        public float MusicStart;

        public float MusicEnd;
        
        public const float DefaultClock = 24f;

        public float Duration
        {
            get
            {
                float end = End;
                if (End < Start) ++end;
                return end - Start;
            }
        }

        public DayPhase(float start, float end, float mStart, float mEnd)
        {
            Start = start / DefaultClock;
            End = end / DefaultClock;
            MusicStart = mStart / DefaultClock;
            MusicEnd = mEnd / DefaultClock;
        }
    }
}