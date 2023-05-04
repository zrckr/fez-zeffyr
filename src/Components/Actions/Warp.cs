using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class Warp : PlayerAction
    {
        [Export] public AudioStreamSample WarpSound;
        
        private enum Phases
        {
            None,
            Rise,
            Accelerate,
            Warp,
            Decelerate,
            FadeOut,
            LevelChange,
            FadeIn,
        }

        private ChangeLevelArea _trigger;
        private Vector3 _camOrigin;
        private Phases _phase;
        private float _sinceRisen;
        private float _sinceStarted;

        protected override void TransitionAttempts()
        {
            if (Player.Action != ActionType.GateWarp && Player.ChangeArea != null &&
                Player.ChangeArea.Visible && Player.ChangeArea.Warp != null && 
                FzInput.IsJustPressed(InputAction.Up))
            {
                Player.Action = ActionType.GateWarp;
            }
        }

        protected override void OnEnter()
        {
            Player.FacingDirection = Direction.Left;
            GameCamera.CanFollow = GameCamera.CanRotate = false;
            MusicManager.FadeVolume(MusicManager.MusicVolume, 0f, 3f);
            AudioPlayer.Stream = WarpSound;
            AudioPlayer.Play();
            
            _phase = Phases.Rise;
            _sinceRisen = _sinceStarted = 0f;
            _camOrigin = GameCamera.Origin;
            _trigger = Player.ChangeArea;
        }

        protected override void OnAct(float delta)
        {
            Vector3 gateOrigin = _trigger.Warp.GlobalTransform.origin;
            switch (_phase)
            {
                case Phases.Rise:
                    _phase = Phases.Accelerate;
                    break;
                
                case Phases.Accelerate:
                case Phases.Warp:
                    _sinceRisen += delta;
                    _sinceStarted += delta;
                    
                    Vector3 vector3 = Player.Axes.Up * 4f * Easing.In.Cubic(_sinceStarted / 4.5f);
                    
                    Player.Origin = Player.Origin.LinearInterpolate(
                        _trigger.Warp.GlobalTransform.origin - Player.Axes.Forward * 3f + vector3, 0.0375f);
                    
                    GameCamera.Origin = _camOrigin + vector3 * 0.4f;
                    AnimationPlayer.PlaybackSpeed = Mathf.Max(_sinceRisen / 2f, 0f);
                    
                    if (_phase != Phases.Warp && _sinceStarted > 4f)
                    {
                        _phase = Phases.Warp;
                        FadeManager.DoFade(Colors.Transparent, Colors.White, 0.5f);
                        FadeManager.Faded += () =>
                        {
                            _sinceStarted = 0f;
                            _phase = Phases.Decelerate;
                            Player.Visible = false;
                            FadeManager.DoFade(Colors.White, Colors.Transparent, 1f);
                        };
                    }
                    break;
                
                case Phases.Decelerate:
                    _sinceStarted += delta;
                    Player.Origin = Player.Origin.LinearInterpolate(
                        gateOrigin - Player.Axes.Forward * 3f, 0.025f);
                    if (_sinceStarted > 1f)
                    {
                        _phase = Phases.FadeOut;
                        _sinceStarted = 0f;
                    }
                    break;
                
                case Phases.FadeOut:
                case Phases.LevelChange:
                    _sinceStarted += delta;
                    Player.Origin = Player.Origin.LinearInterpolate(
                        gateOrigin - Player.Axes.Forward * 3f, 0.025f);
                    
                    if (_phase != Phases.LevelChange && _sinceStarted > 2.25f)
                    {
                        _phase = Phases.LevelChange;
                        _sinceStarted = 0f;
                        FadeManager.DoSquare(Colors.Black, GetViewport().Size / 2f, 1f, false);
                        FadeManager.Faded += DoLoad;
                    }
                    break;
                case Phases.FadeIn:
                    _sinceStarted += delta;
                    if (_sinceStarted > 2.25f)
                    {
                        _sinceStarted = 0f;
                        _phase = Phases.None;
                    }
                    break;
            }
        }

        private void DoLoad()
        {
            GameState.ChangeLevel(_trigger.NextLevel, _trigger.VolumeId);
                
            Vector3 floor = Level.GetNode<Spatial>(_trigger.VolumeId.ToString()).GlobalTransform.origin;
            GameState.SaveData.View = _trigger.ViewAfterWarp;
            GameState.SaveData.Floor = new[] {floor.x, floor.y, floor.z};

            Player.Respawn.Checkpoint = null;
            Player.Respawn.LoadAtCheckpoint();

            GameCamera.Origin = Player.Origin + Player.Axes.Up * (1f + Player.Size.y);
            GameCamera.CanFollow = GameCamera.CanRotate = true;
            Player.Visible = true;

            _sinceStarted = 0f;
            _phase = Phases.FadeIn;
        }

        protected override bool IsAllowed(ActionType type) => type == ActionType.GateWarp;
    }
}