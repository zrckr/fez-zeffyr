using Godot;
using Zeffyr.Structure;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class ClimbOver : PlayerAction
    {
        protected override void OnEnter()
        {
            Vector3 origin = Player.Origin;
            
            origin *= Mathz.TrixelsPerUnit;
            origin = origin.Round();
            origin /= Mathz.TrixelsPerUnit;
            origin += Player.Axes.Facing * 0.5f;

            Player.Origin = origin;
            Player.Velocity = Vector3.Zero;
            Signals.EmitSignal(nameof(ActionSignals.ClimbedOverLadder));
        }

        protected override void OnAct(float delta)
        {
            if (!AnimationPlayer.IsPlaying())
            {
                Player.HeldBody = null;
                Player.Action = ActionType.Idle;
                Player.Origin += Player.Axes.Facing * (2 * Mathz.Trixel);
                Player.Velocity = Vector3.Down;
            }
        }

        protected override bool IsAllowed(ActionType type)
            => type == ActionType.ClimbOver;
    }
}