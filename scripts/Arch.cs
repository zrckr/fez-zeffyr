using Godot;
using Zeffyr.Components;
using Zeffyr.Structure.Physics;
using Zeffyr.Tools;

namespace Zeffyr.Scripts
{
    public class Arch : Level
    {
        private Pickup _create;
        private Area _checkCrate;
        private MeshInstance _pushSwitch;
        private GridMap _upDown;
        private Tween _tween;
        private Collectable _antiCube;

        private Vector3 _upDownOrigin;
        private float _sinceReady;

        public override void _Ready()
        {
            base._Ready();
            
            _tween = GetNode<Tween>(nameof(Tween));
            _pushSwitch = GetNode<MeshInstance>("Groups/PushSwitch");
            _upDown = GetNode<GridMap>("Groups/UpDown");
            _checkCrate = GetNode<Area>("Volumes/CheckCrate");
            _antiCube = GetNode<Collectable>("Items/AntiCube");
            _create = GetNode<Pickup>("Items/Crate");

            _sinceReady = 0f;
            _antiCube.Enable = false;
            _upDownOrigin = _upDown.GlobalTransform.origin;

            Vector3 size = _checkCrate.ShapeOwnerGetExtents(0);
            size.z = CollisionHelper.MaxDepth;
            
            _checkCrate.ShapeOwnerSetExtents(0, size);
            _checkCrate.Connect("body_entered", this, nameof(OnCrateEntered));
        }

        private async void OnCrateEntered(Node body)
        {
            ulong id1 = body.GetInstanceId();
            ulong id2 = _create.GetInstanceId();
            Vector3 xzMask = new Vector3(1, 0, 1);
            
            if (id1 == id2 && _create.EnableCollision)
            {
                _create.Velocity = Vector3.Zero;
                Vector3 initial = _create.Origin;
                Vector3 final = _checkCrate.Translation * xzMask + _create.Origin * Vector3.Up;
                _tween.InterpolateProperty(_create, "Origin", initial, final, 2f);
                
                initial = _pushSwitch.Translation;
                final = _pushSwitch.Translation + Vector3.Down;
                _tween.InterpolateProperty(_pushSwitch, "translation", initial, final, 2f);

                initial = _create.Translation;
                final = _create.Translation + Vector3.Down;
                _tween.InterpolateProperty(_create, "translation", initial, final, 2f);
                
                _tween.Start();
                await ToSignal(_tween, "tween_all_completed");

                _antiCube.Enable = true;
                ResolvePuzzle(_checkCrate);
            }
        }

        public override void _Process(float delta)
        {
            _sinceReady += delta * 0.5f;
            float step = Easing.InOut.Sine(_sinceReady);

            Transform transform = _upDown.GlobalTransform;
            transform.origin = _upDownOrigin.LinearInterpolate(_upDownOrigin + Vector3.Up, step);
            _upDown.GlobalTransform = transform;

            _checkCrate.Rotation = GameCamera.Rotation;
        }
    }
}