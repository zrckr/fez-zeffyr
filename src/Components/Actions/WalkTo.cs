using Godot;
using System;
using Zeffyr.Structure;
using Zeffyr.Structure.Physics;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class WalkTo : PlayerAction
    {
        public ActionType NextAction { get; set; }
        public Func<Vector3> NextOrigin { get; set; }

        private MoveHelper _moveHelper;
        private Direction _originalDirection;
        private bool _stoppedByWall;
        
        protected override void OnEnter()
        {
            _originalDirection = Player.FacingDirection;
            _moveHelper = WalkRun.Default;
            _stoppedByWall = false;
        }
        
        protected override void OnAct(float delta)
        {
            AnimationPlayer.PlaybackSpeed = _moveHelper.IsRunning ? 1.25f : 1f;
            Player.FacingDirection = _originalDirection;
            
            float directionTo = (NextOrigin() - Player.Origin).Dot(Player.Axes.Right);
            int sign = directionTo < 0f ? -1 : 1;
            Player.FacingDirection = directionTo < 0.0f ? Direction.Left : Direction.Right;
            
            _stoppedByWall = Player.Wall.Collider != null;
            if (!Mathf.IsEqualApprox(directionTo, 0.0f, Mathz.Trixel) && !_stoppedByWall)
            {
                _moveHelper.Update(delta, sign * 0.75f);
                Player.Velocity += Mathf.Min(Mathf.Abs(Player.Velocity.x), Mathf.Abs(directionTo)) * sign * Vector3.Right;
            }
            else
            {
                Player.FacingDirection = _originalDirection;
                Player.Action = this.NextAction;
                Player.Velocity *= Vector3.Up;
            
                if (!_stoppedByWall)
                {
                    Player.Origin = NextOrigin();
                    Player.GlobalVelocity = PhysicsHelper.HugResponse(Player);
                }
                NextOrigin = null;
                NextAction = ActionType.None;
                Signals.EmitSignal(nameof(ActionSignals.WalkedTo));
            }
        }
        
        protected override bool IsAllowed(ActionType type) => type == ActionType.WalkTo;
    }
}