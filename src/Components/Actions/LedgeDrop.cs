using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Structure.Physics;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class LedgeDrop : PlayerAction
    {
        [Export] public AudioStreamSample DropFromLedgeSound;
        [Export] public AudioStreamSample DropFromVineSound;
        [Export] public AudioStreamSample DropFromLadderSound;

        public override void _Ready()
        {
            base._Ready();
            this.InjectNodes();
        }

        protected override void TransitionAttempts()
        {
            switch (Player.Action)
            {
                case ActionType.ClimbFront:
                case ActionType.ClimbBack:
                case ActionType.ClimbSide:
                case ActionType.ClimbBackSideways:
                case ActionType.ClimbFrontSideways:
                case ActionType.CornerGrab:
                case ActionType.ToCornerFront:
                case ActionType.ToCornerBack:
                case ActionType.FromCornerBack:
                case ActionType.ShimmyFront:
                case ActionType.ShimmyBack:
                    if ((FzInput.IsJustPressed(InputAction.Jump) && Mathf.IsZeroApprox(FzInput.Movement.x)) || 
                        (FzInput.IsJustPressed(InputAction.Jump) && FzInput.AnyPressed(InputAction.Down) && Player.Action.IsOnLedge()))
                    {
                        Player.HeldBody = null;
                        Player.Action = ActionType.DropDown;
                        Player.CanDoubleJump = false;
                    }
                    break;
            }
        }

        protected override void OnEnter()
        {
            bool isVine = Player.HeldBody.GetLayer().HasFlag(PhysicsLayer.Vine);
            if (Player.LastAction.IsClimbing())
            {
                AudioPlayer.Stream = (isVine) ? DropFromVineSound : DropFromLadderSound;
            }
            else if (Player.LastAction.IsOnLedge())
            {
                AudioPlayer.Stream = DropFromLedgeSound;
                Signals.EmitSignal(nameof(ActionSignals.DroppedLedge));

                if (Player.LastAction == ActionType.CornerGrab || Player.LastAction == ActionType.CornerLowerTo)
                    Player.Origin -= Player.Axes.Facing * 0.5f;
                
                Player.InBackground = PhysicsHelper.DetermineInBackground(Player);
            }
            AudioPlayer.Play();

            if (Player.LastAction == ActionType.CornerGrab)
            {
                Player.Origin -= Player.Axes.Facing * Mathz.HalfTrixel * 15f;
                if (Player.IsClimbing && isVine)
                {
                    Player.Action = ActionType.ClimbSide;
                    return;
                }
            }
            Player.Action = ActionType.Fall;
        }

        protected override bool IsAllowed(ActionType type) => type == ActionType.DropDown;
    }
}