using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Physics;

namespace Zeffyr.Components.Actions
{
    public class SuckedIn : PlayerAction
    {
        protected override void TransitionAttempts()
        {
            switch (Player.Action)
            {
                case ActionType.SuckedIn:
                case ActionType.OpenTreasure:
                case ActionType.FindTreasure:
                case ActionType.EnterDoor:
                case ActionType.CarryEnter:
                case ActionType.CarryHeavyEnter:
                case ActionType.EnterDoorSpin:
                case ActionType.CarryEnterDoorSpin:
                case ActionType.CarryHeavyEnterDoorSpin:
                    break;
                default:
                    Area blackHole = SpaceState.CastPoint<Area>(Player.GlobalTransform, PhysicsLayer.Deadly);

                    if (blackHole != null &&
                        blackHole.GetLayer().HasFlag(PhysicsLayer.Deadly) &&
                        blackHole.GetGroups().Contains("BlackHoles"))
                    {
                        Vector3 origin = blackHole.GlobalTransform.origin;
                        Player.Action = ActionType.SuckedIn;
                        Player.Origin = Player.Origin * Player.Axes.XyMask + 
                                        origin * Player.Axes.ZMask + Player.Axes.Forward;
                    }
                    break;
            }
        }

        protected override void OnEnter()
        {
            Player.FacingDirection = Player.FacingDirection.GetOpposite();
            Player.Floor = null;
            Player.Velocity = Player.Velocity.Sign() * 0.5f;
            
            if (Player.CarriedBody != null)
            {
                Player.CarriedBody.EnableCollision = true;
                Player.CarriedBody = null;
            }
        }

        protected override void OnAct(float delta)
        {
            if (!AnimationPlayer.IsPlaying())
                Player.Respawn.Load();
        }

        protected override bool IsAllowed(ActionType type) => type == ActionType.SuckedIn;
    }
}