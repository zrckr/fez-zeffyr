using Godot;
using Zeffyr.Managers;
using Zeffyr.Structure;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class FindTreasure : PlayerAction
    {
        [Export] public float TweenTime = 0.25f;

        private Vector3 _origin;
        private Spatial _treasure;
        private float _elapsed;
        private float _lastCameraSize;
        
        private string[] _chords = new string[]
        {
            "c_maj",
            "csharp_maj",
            "d_maj",
            "dsharp_maj",
            "e_maj",
            "f_maj",
            "fsharp_maj",
            "g_maj",
            "gsharp_maj",
            "a_maj",
            "asharp_maj",
            "b_maj",
        };

        protected override void OnEnter()
        {
            _elapsed = 0f;
            _treasure = GetNode<Spatial>("../../Treasure");
            _treasure.Visible = true;
            _lastCameraSize = GameCamera.Size;
            _origin = Player.Origin;
            
            Tween.StopAll();
            Tween.InterpolateProperty(GameCamera,  "Size",
                _lastCameraSize, _lastCameraSize * 0.5f, TweenTime);

            Tween.InterpolateProperty(_treasure, "rotation_degrees",
                new Vector3(-45, 0, 45), new Vector3(-45, 720, 45), 1.6f,
                Tween.TransitionType.Quart, Tween.EaseType.In);
            Tween.InterpolateProperty(_treasure, "rotation_degrees",
                new Vector3(-45, 720, 45), new Vector3(-45, 1440, 45), 2.4f,
                Tween.TransitionType.Quart, Tween.EaseType.Out, 1.6f);
            Tween.Start();
            
            if (Player.CarriedBody != null)
            {
                Player.Action = Player.CarriedBody.IsHeavy ? ActionType.CarryHeavyIdle : ActionType.CarryIdle;
                AnimationPlayer.PlaybackSpeed = 0f;
            }
            else
            {
                Player.Action = ActionType.FindTreasure;
            }
            PlayChordSound();
        }

        protected override void OnAct(float delta)
        {
            _elapsed += delta;
            Player.Origin = _origin;
            Player.Velocity = Vector3.Zero;

            GameCamera.CanRotate = GameCamera.CanFollow = false;
            Player.CollectedTreasure.GlobalTransform = _treasure.GlobalTransform;
            
            if (_elapsed > 4f)
            {
                _treasure.Visible = false;
                Tween.StopAll();
                Tween.InterpolateProperty(GameCamera, "Size",
                    GameCamera.Size, _lastCameraSize, 0.25f);
                Tween.Start();

                if (Player.CollectedTreasure is CubeAssembly)
                {
                    GameState.SaveData.BigCubes += 1;
                    Signals.EmitSignal(nameof(ActionSignals.CollectedBigCube));
                }
                else if (Player.CollectedTreasure is Collectable collectable)
                {
                    if (collectable.ItemType == Collectable.Type.BigCube)
                    {
                        GameState.SaveData.BigCubes += 1;
                        Player.CollectedTreasure.QueueFree();
                        Signals.EmitSignal(nameof(ActionSignals.CollectedBigCube));
                    }
                    else if (collectable.ItemType == Collectable.Type.AntiCube)
                    {
                        GameState.SaveData.AntiCubes += 1;
                        Player.CollectedTreasure.QueueFree();
                        Signals.EmitSignal(nameof(ActionSignals.CollectedAntiCube));
                    }
                }
                GameState.EmitSignal(nameof(GameStateManager.OnHudChanged));

                if (Player.CarriedBody is null)
                    Player.Action = ActionType.Idle;
                
                Player.CollectedTreasure = null;
                GameState.SaveGame();
                GameCamera.CanRotate = GameCamera.CanFollow = true;
            }
        }

        private void PlayChordSound()
        {
            string name = _chords.Choose();
            string path = $"res://assets/Sounds/Collects/SplitUpCube/assemble_{name}.wav";
           
            AudioPlayer.Stream = ResourceLoader.Load<AudioStreamSample>(path);
            AudioPlayer.Play();
        }

        protected override bool IsAllowed(ActionType type) => Player.CollectedTreasure != null;
    }
}