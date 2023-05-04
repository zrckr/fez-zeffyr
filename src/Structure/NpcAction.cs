namespace Zeffyr.Structure
{
    public enum NpcAction
    {
        None,
        Idle,
        Idle2,
        Idle3,
        Walk,
        Turn,
        Talk,
        Burrow,
        Hide,
        ComeOut,
    }

    public static class NpcActionExtensions
    {
        public static bool AllowsRandomChange(this NpcAction action)
        {
            switch (action)
            {
                case NpcAction.Idle:
                case NpcAction.Idle3:
                case NpcAction.Walk:
                    return true;
                default:
                    return false;
            }
        }

        public static bool Loops(this NpcAction action)
        {
            switch (action)
            {
                case NpcAction.Idle2:
                case NpcAction.Turn:
                case NpcAction.Burrow:
                case NpcAction.Hide:
                case NpcAction.ComeOut:
                    return false;
                default:
                    return true;
            }
        }

        public static bool IsSpecialIdle(this NpcAction action) => action == NpcAction.Idle2 || action == NpcAction.Idle3;
    }
}