using Godot;
using Zeffyr.Managers;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Structure.Physics;

namespace Zeffyr.Components.Actions
{
    public class OpenTreasure : PlayerAction
    {
        [Export] public AudioStreamSample TreasureGetSound;

        private Collectable _treasure;
        private Spatial _itemPath;
        private Spatial _cameraPath;
        private Collectable _treasureItem;
        private Vector3 _oldRotation;

        protected override void TransitionAttempts()
        {
            switch (Player.Action)
            {
                case ActionType.OpenTreasure:
                case ActionType.FindTreasure:
                case ActionType.ReadListen:
                case ActionType.AirPanic:
                case ActionType.Dying:
                case ActionType.GateWarp:
                case ActionType.FirstPerson:
                    break;
                default:
                    if (Player.OnFloor && !Player.InBackground)
                    {
                        _treasure = SpaceState.CastPoint<Collectable>(Player.GlobalTransform, PhysicsLayer.Actors);
                        
                        Axes axes = new Axes(_treasure?.GlobalTransform.basis ?? Basis.Identity);
                        float dot = axes.Forward.Dot(Player.Axes.Forward);

                        if (_treasure != null &&
                            _treasure.Monitorable &&
                            _treasure.ItemType == Collectable.Type.TreasureChest &&
                            FzInput.IsJustPressed(InputAction.GrabThrow) &&
                            dot >= 1f)
                        {
                            WalkTo.NextOrigin = GetOriginToTreasure;
                            WalkTo.NextAction = ActionType.OpenTreasure;
                            Player.Action = ActionType.WalkTo;
                        }
                    }
                    break;
            }
        }

        protected override void OnEnter()
        {
            GameState.MakeNodeInactive(_treasure);
            Player.Velocity = Vector3.Zero;
            Signals.EmitSignal(nameof(ActionSignals.OpenedTreasure));
            GameCamera.CanRotate = GameCamera.CanFollow = false;

            MusicManager.FadeVolume(1f, 0.125f, 2f);
            AudioPlayer.Stream = TreasureGetSound;
            AudioPlayer.Play();

            _treasureItem = _treasure.TreasureItem.Instance<Collectable>();
            _treasureItem.Enable = false;
            _treasure.AddChild(_treasureItem);
            
            _itemPath = _treasure.GetNode<Spatial>("ItemPath");
            _cameraPath = _treasure.GetNode<Spatial>("CameraPath");
            
            _treasure.GetNode<AnimationPlayer>("Animation").Play("default");
            _treasure.Monitorable = false;
            _oldRotation = GameCamera.Rotation;
        }

        protected override void OnAct(float delta)
        {
            Player.Rotation = GameCamera.Rotation = _oldRotation + _cameraPath.Rotation;
            GameCamera.Origin = GameCamera.Origin.LinearInterpolate(Player.Origin, 0.5f);

            _treasureItem.GlobalTransform = _itemPath.GlobalTransform;
            _treasureItem.Visible = _itemPath.Visible;
            
            AnimationPlayer.PlaybackSpeed = 0.9f;
            if (!AnimationPlayer.IsPlaying())
            {
                _treasureItem.QueueFree();
                Player.Action = ActionType.Idle;
                MusicManager.FadeVolume(0.125f, 1f, 2f);
            }
        }

        protected override void OnEnd()
        {
            switch (_treasureItem.ItemType)
            {
                case Collectable.Type.GoldenKey:
                    GameState.SaveData.Keys += 1;
                    break;
                    
                case Collectable.Type.BigCube:
                    GameState.SaveData.BigCubes += 1;
                    Signals.EmitSignal(nameof(ActionSignals.CollectedBigCube));
                    break;
                
                case Collectable.Type.AntiCube:
                    GameState.SaveData.AntiCubes += 1;
                    Signals.EmitSignal(nameof(ActionSignals.CollectedAntiCube));
                    break;
            }
            GameState.EmitSignal(nameof(GameStateManager.OnHudChanged));
            GameState.SaveGame();
            GameCamera.CanRotate = GameCamera.CanFollow = true;
        }

        private Vector3 GetOriginToTreasure() => _treasure.GlobalTransform.origin * Player.Axes.XMask + 
                                                 Player.Origin * Player.Axes.YzMask;

        protected override bool IsAllowed(ActionType type) => type == ActionType.OpenTreasure;
    }
}