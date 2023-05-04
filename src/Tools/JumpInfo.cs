using static Godot.Mathf;

namespace Zeffyr.Tools
{
    public record JumpInfo
    {
        public float Fall;

        public float Jump;

        public float Termination;

        public JumpInfo(float minHeight, float maxHeight, float duration)
        {
            Fall = (-2f * maxHeight) / (duration * duration);
            Jump = Sqrt(-2f * Fall * maxHeight);
            Termination = Sqrt(Jump * Jump + 2f * Fall * (maxHeight - minHeight));
        }
    }
}