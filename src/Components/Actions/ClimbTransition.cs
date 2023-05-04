using Godot;
using System;
using Zeffyr.Structure;
using Zeffyr.Structure.Physics;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class ClimbTransition : PlayerAction
    {
        [Export] public AudioStreamSample GrabLadderSound;
        [Export] public AudioStreamSample GrabVineSound;

        [Export] public float BackFactor = 0.16f;
        [Export] public float SideFactor = 0.32f;

        private float? _sinceGrabbed;

        protected override void OnEnter()
        {
            Player.Velocity = Vector3.Zero;
            _sinceGrabbed = 0f;
        }

        protected override void OnAct(float delta)
        {
            bool isVine = Player.HeldBody.GetLayer().HasFlag(PhysicsLayer.Vine);
            if (_sinceGrabbed.HasValue)
            {
                float value = Player.Action switch
                {
                    ActionType.JumpToClimb => BackFactor,
                    ActionType.JumpToClimbSide => SideFactor,
                    _ => float.MaxValue
                };

                _sinceGrabbed = _sinceGrabbed.Value + delta;
                if (_sinceGrabbed.Value >= value)
                {
                    _sinceGrabbed = null;
                    Player.Velocity = Vector3.Zero;
                    AudioPlayer.Stream = (isVine) ? GrabVineSound : GrabLadderSound;
                    AudioPlayer.Play();
                }
            }

            bool nextIsLadder = !isVine &&
                (Player.NextAction.In(ActionType.ClimbFront, ActionType.ClimbBack, ActionType.ClimbSide));
            
            if (nextIsLadder || Player.NextAction == ActionType.ClimbSide)
            {
                Vector3 to = Player.Axes.Up * Player.Origin +
                             Player.Axes.XzMask * Player.HeldBody.GlobalTransform.origin;

                float step = AnimationPlayer.CurrentAnimationPosition / AnimationPlayer.CurrentAnimationLength;
                Player.Origin = Player.Origin.LinearInterpolate(to, Easing.In.Quad(step));
            }

            if (Player.Velocity.y > 0f)
            {
                Player.Velocity += Vector3.Down * 10f / 3f * delta;
            }

            if (!AnimationPlayer.IsPlaying())
            {
                Player.Action = Player.NextAction != ActionType.None
                    ? Player.NextAction
                    : throw new InvalidOperationException("How did you get here...?");
                Player.NextAction = ActionType.None;
            }
        }

        protected override bool IsAllowed(ActionType type)
            => type.In(
                ActionType.IdleToClimbFront,
                ActionType.IdleToClimbSide,
                ActionType.IdleToClimbBack,
                ActionType.JumpToClimb,
                ActionType.JumpToClimbSide);
    }
}