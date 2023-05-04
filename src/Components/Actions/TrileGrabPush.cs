using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Physics;
using Zeffyr.Structure.Input;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class TrileGrabPush : PlayerAction
    {
        [Export(PropertyHint.Range, "0,1,0.1")]
        public float PushFactor = 0.5f;
        
        private MoveHelper _moveHelper;
        private int _pushingSign;

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
                case ActionType.Teeter:
                    if (Player.OnFloor && !Player.InBackground && !Mathf.IsZeroApprox(FzInput.Movement.x) && 
                        Player.Wall.Collider is Pickup pickup)
                    {
                        if (pickup.GetLayer().HasFlag(PhysicsLayer.AllSides) && pickup.Visible && pickup.OnFloor && 
                            Mathf.IsZeroApprox(pickup.Velocity.x))
                        {
                            Player.Action = ActionType.Grab;
                            Player.PushedBody = pickup;
                        }
                    }
                    break;
            }
        }

        protected override void OnEnter()
        {
            Player.Velocity *= Vector3.Up;
            _pushingSign = Player.FacingDirection.Sign();
            _moveHelper = new MoveHelper(Player.PushedBody, Player.DefaultSpeed * PushFactor, 0, float.MaxValue);
        }

        protected override void OnAct(float delta)
        {
            if (Player.PushedBody == null || !Player.PushedBody.Visible ||
                _pushingSign == -Mathf.Sign(FzInput.Movement.x) || !Player.OnFloor)
            {
                Player.Action = ActionType.Idle;
                Player.PushedBody = null;
                return;
            }

            Vector3 offset = Player.PushedBody.Size + Player.Size + Vector3.One * Mathz.Trixel;
            Player.Origin = Player.Axes.YMask * Player.Origin +
                            Player.Axes.XzMask * Player.PushedBody.Origin -
                            Player.Axes.Right * _pushingSign * offset;
            
            switch (Player.Action)
            {
                case ActionType.Grab:
                    if (!Mathf.IsZeroApprox(FzInput.Movement.x) && !AnimationPlayer.IsPlaying())
                    {
                        Player.Action = ActionType.Push;
                    }
                    break;

                case ActionType.Push:
                    _moveHelper.Update(delta, FzInput.Movement.x);
                    if (Mathf.IsZeroApprox(Player.PushedBody.Velocity.x))
                    {
                        Player.Action = ActionType.Grab;
                        AnimationPlayer.Seek(AnimationPlayer.CurrentAnimationLength, true);
                    }
                    break;
            }
        }

        protected override bool IsAllowed(ActionType type) => type == ActionType.Grab || type == ActionType.Push;
    }
}