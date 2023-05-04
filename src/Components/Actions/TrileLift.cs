using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Structure.Physics;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class TrileLift : PlayerAction
    {
        protected override void TransitionAttempts()
        {
            switch (Player.Action)
            {
                case ActionType.Idle:
                case ActionType.IdlePlay:
                case ActionType.IdleSleep:
                case ActionType.IdleLookAround:
                case ActionType.IdleYawn:
                case ActionType.Walk:
                case ActionType.Run:
                case ActionType.Slide:
                case ActionType.Land:
                case ActionType.Grab:
                case ActionType.Push:
                case ActionType.Teeter:
                    if (!Player.InBackground && Player.OnFloor && FzInput.IsPressed(InputAction.GrabThrow))
                    {
                        Pickup possiblePickup = SpaceState.CastRect<Pickup>(Player.GlobalTransform,
                            Player.Size * 2f, PhysicsLayer.Actors).First;
                        
                        Pickup carried = Player.PushedBody ?? possiblePickup;
                        if (carried != null)
                        {
                            ActionType action = carried.IsHeavy ? ActionType.LiftHeavy : ActionType.LiftTrile;
                            if (Player.Action == ActionType.Grab)
                            {
                                Player.CarriedBody = carried;
                                Player.Action = action;
                            }
                            else
                            {
                                WalkTo.NextAction = action;
                                WalkTo.NextOrigin = GetDestination;
                                Player.Action = ActionType.WalkTo;
                                Player.PushedBody = carried;
                            }
                        }
                    }
                    break;
            }
        }

        private Vector3 GetDestination()
        {
            return Player.Axes.YzMask * Player.Origin +
                   Player.Axes.XMask * Player.PushedBody.Origin -
                   Player.Axes.Facing * (Player.PushedBody.Size + Player.Size + Vector3.One * Mathz.HalfTrixel);
        }

        protected override void OnEnter()
        {
            if (Player.CarriedBody is null && Player.PushedBody is null)
            {
                Player.Action = ActionType.Idle;
            }
            else
            {
                if (Player.PushedBody != null)
                {
                    Player.CarriedBody = Player.PushedBody;
                    Player.PushedBody = null;
                }
                
                Player.CarriedBody!.EnableCollision = false;
                Player.Velocity *= Vector3.Up;
                Signals.EmitSignal(nameof(ActionSignals.LiftedObject));
            }
        }

        protected override void OnAct(float delta)
        {
            if (!AnimationPlayer.IsPlaying())
            {
                NodeExtensions.AdoptChild((Node) Player, Player.CarriedBody);
                Player.Action = Player.CarriedBody.IsHeavy
                    ? ActionType.CarryHeavyIdle : ActionType.CarryIdle;
            }
        }

        protected override bool IsAllowed(ActionType type)
            => type == ActionType.LiftTrile || type == ActionType.LiftHeavy;
    }
}