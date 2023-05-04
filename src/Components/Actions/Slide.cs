using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;

namespace Zeffyr.Components.Actions
{
    public class Slide : PlayerAction
    {
        protected override void TransitionAttempts()
        {
            switch (Player.Action)
            {
                case ActionType.Idle:
                case ActionType.Walk:
                case ActionType.Run:
                case ActionType.IdlePlay:
                case ActionType.IdleSleep:
                case ActionType.IdleLookAround:
                case ActionType.IdleYawn:
                    if (!Mathf.IsZeroApprox(Player.Velocity.x) && Mathf.IsZeroApprox(FzInput.Movement.x))
                        Player.Action = ActionType.Slide;
                    break;
            }
        }

        protected override void OnAct(float delta)
        {
            if (Player.CarriedBody != null)
                WalkRun.Carry.Update(delta, 0f);
            else
                WalkRun.Default.Update(delta, 0f);
        }

        protected override bool IsAllowed(ActionType type) => type == ActionType.Slide;
    }
}