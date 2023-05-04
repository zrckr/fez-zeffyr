using Godot;
using Zeffyr.Components.Actions;
using Zeffyr.Structure;
using Zeffyr.Structure.Physics;
using Zeffyr.Tools;

namespace Zeffyr.Components
{
    public class Pickup : KinematicBody, IPhysicsEntity
    {
        [Export] public bool IsHeavy;

        [Export] public bool EnableCollision = true;

        [Export] public SurfaceType SurfaceType;

        public Vector3 Velocity { get; set; }

        public Vector3 GlobalVelocity
        {
            get => Transform.basis.Xform(Velocity);
            set => Velocity = Transform.basis.XformInv(value);
        }

        public Vector3 Origin
        {
            get => GlobalTransform.origin;
            set => GlobalTransform = new Transform()
            {
                basis = GlobalTransform.basis,
                origin = value,
            };
        }

        public Vector3 Size => this.ShapeOwnerGetExtents(0);

        public bool InBackground { get; set; }

        public PhysicsBody Floor { get; set; }

        public CollisionInfo Wall { get; set; }

        public CollisionInfo Ceiling { get; set; }

        public bool IsSliding => OnFloor && !Mathf.IsZeroApprox(Velocity.x);

        public bool IsSwimming { get; private set; }

        public bool IsClimbing => false;

        public bool OnFloor => IsOnFloor();

        public bool OnWall => IsOnWall();

        public bool OnCeiling => IsOnCeiling();

        private Water Water => ((Level) GetTree().CurrentScene).Water;
        
        private float Gravity => (float) ProjectSettings.GetSetting("physics/3d/default_gravity") * -1f;

        private Transform _respawnOrigin;
        private uint _collisionLayer;
        private float _sinceHeavyDrowned;

        public override void _Ready()
        {
            this.InjectNodes();
            _respawnOrigin = GlobalTransform;
            _collisionLayer = CollisionLayer;
            AddToGroup(SurfaceType.ToString());
        }

        public override void _Process(float delta)
        {
            GetNode<CollisionShape>("CollisionShape").Disabled = !EnableCollision;
            SetPhysicsProcess(EnableCollision);
            CollisionLayer = EnableCollision ? _collisionLayer : 0;
        }

        public override void _PhysicsProcess(float delta)
        {
            if (!OnFloor)
            {
                bool oldSwimming = IsSwimming;
                IsSwimming = Water != null && Origin.y < Water.Height - 0.5f;
                if (IsSwimming)
                {
                    if (!oldSwimming)
                        Velocity *= new Vector3(1f, 0.25f, 1f);
                    TryFloatOnWater(delta);
                }
                else
                {
                    Vector3 velocity = Velocity;
                    velocity.y = Mathf.Max(Gravity, velocity.y + delta * Gravity);
                    Velocity = velocity;
                }
            }

            Vector3? floorDistance = PhysicsHelper.TestAndCollide(this, OnFloor, delta);
            if (!InBackground)
            {
                GlobalVelocity = PhysicsHelper.HugResponse(this);
                GlobalTransform = PhysicsHelper.TryCorrectDepth(this, floorDistance);
            }

            if (GlobalVelocity.LengthSquared() > 0)
            {
                GlobalVelocity = MoveAndSlide(GlobalVelocity, Transform.basis.y);
                if (GlobalTransform.origin.Dot(GlobalTransform.basis.y) <= -100f || _sinceHeavyDrowned >= 10f)
                {
                    _sinceHeavyDrowned = 0f;
                    GlobalTransform = _respawnOrigin;
                }
            }
        }

        private void TryFloatOnWater(float delta)
        {
            float height = Water.Height - 0.5f;
            Axes axes = new Axes(GlobalTransform.basis);
            
            if (!Mathf.IsEqualApprox(Origin.y, height, Mathz.Trixel) && !IsHeavy)
            {
                float diff = height - Origin.y;
                Velocity += Swim.UpFactor * delta * Vector3.Up;

                if (diff > Swim.DiffFactor)
                {
                    Velocity += diff * Swim.StableFactor * Vector3.Up;
                }
                else
                {
                    Origin = Origin * axes.XzMask + axes.Up * height;
                }
                _sinceHeavyDrowned = 0f;
            }
            else if (IsHeavy || Water.Type.In(LiquidType.Lava, LiquidType.Sewer))
            {
                _sinceHeavyDrowned += delta;
                Vector3 velocity = Velocity;
                velocity.y = Mathf.Max(Gravity / 4f, velocity.y + delta * Gravity / 4f);
                Velocity = velocity;
            }
            else if (Random.Probability(delta))
            {
                Velocity += Vector3.Down * 0.25f;
            }
        }

        public override string ToString() => $"IsHeavy: {IsHeavy}, Parent: {GetParent().Name}";
    }
}