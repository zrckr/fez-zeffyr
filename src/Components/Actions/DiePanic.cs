using Godot;
using System.Collections.Generic;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class DiePanic : PlayerAction
    {
        [Export] public float FreeFallStart = 10f;
        [Export] public float FreeFallEnd = 36f;
        [Export] public float CamPanUp = 5f;
        [Export] public float CamFollowEnd = 27f;

        private bool _wasFollow;
        private float _player;
        private float _leaveFloor;
        private float _respawn;
        private int? _capEnd;

        private const string EndCapsPath = "res://assets/AirPanicEndCaps.json";
        private readonly Dictionary<string, int> _endCaps;

        public DiePanic()
        {
            FileOperations.TryLoadJson(EndCapsPath, out _endCaps);
        }

        protected override void TransitionAttempts()
        {
            _player = Player.Axes.Up.Dot(Player.Origin);
            _leaveFloor = Player.Axes.Up.Dot(Player.Respawn.FloorOriginOnLeave);
            _respawn = Player.Axes.Up.Dot(Player.Respawn.RespawnOrigin);

            float diff = _player - GameCamera.Offset.y;
            float start = _leaveFloor - Player.Respawn.OffsetOnFloorLeave - diff;

            if (!Player.IgnoreDiePanic &&
                !Player.OnFloor &&
                !Player.Action.PreventsFall() &&
                Player.Action != ActionType.SuckedIn &&
                start > FreeFallStart)
            {
                Player.Action = ActionType.AirPanic;
            }
        }

        protected override void OnEnter()
        {
            Player.CarriedBody = (Player.CarriedBody != null) ? null : Player.CarriedBody;
            _wasFollow = GameCamera.CanFollow;
            GameCamera.CanFollow = false;
            
            _capEnd = null;
            if (_endCaps.TryGetValue(GetTree().CurrentScene.Name, out int num))
                _capEnd = num;
        }

        protected override void OnEnd()
        {
            GameCamera.CanFollow = _wasFollow;
        }

        protected override void OnAct(float delta)
        {
            float diff = _player - GameCamera.Offset.y;
            float followEnd = (_leaveFloor - Player.Respawn.OffsetOnFloorLeave - diff);
            float radius = GameCamera.PixelsPerUnit / GameCamera.Aspect;
            float fallEnd = FreeFallEnd;
            
            if (_capEnd.HasValue)
                fallEnd = Mathf.Min(FreeFallEnd, _respawn - _capEnd.Value);
            
            GameCamera.CanFollow = (followEnd < CamFollowEnd && 
                                    (!_capEnd.HasValue || GameCamera.GlobalTransform.origin.y - radius / 2f > (_capEnd.Value + 1)));
            if (Player.OnFloor)
            {
                if (Player.Action == ActionType.AirPanic)
                {
                    FzInput.Joypad.Vibrate(Joypad.Motors.RightWeak, 1.0f, 0.5f, Easing.In.Quad);
                    FzInput.Joypad.Vibrate(Joypad.Motors.LeftStrong, 1.0f, 0.35f);
                    Player.Action = ActionType.Dying;
                    Player.Velocity = Vector3.Down;
                }

                if (Player.Action == ActionType.Dying && !AnimationPlayer.IsPlaying())
                    Player.Respawn.Load();
            }
            else if (followEnd > fallEnd)
                Player.Respawn.Load();
        }

        protected override bool IsAllowed(ActionType type) =>
            type.In(ActionType.AirPanic, ActionType.Dying);
    }
}