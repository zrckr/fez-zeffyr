using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Zeffyr.Managers;
using Zeffyr.Structure;
using Zeffyr.Structure.Physics;
using Zeffyr.Structure.Input;
using Zeffyr.Tools;
using Random = Zeffyr.Tools.Random;

namespace Zeffyr.Components
{
    [DebuggerDisplay("{Action} / {Velocity}")]
    public class Gomez : KinematicBody, IPlayerEntity
    {
        [Export] public float DefaultSpeed { get; private set; } = 4.7f;

        [Export] public float LightFactor { get; private set; } = 0.86f;

        [Export] public float HeavyFactor { get; private set; } = 0.5f;

        [Export] public bool IsHatHidden { get; set; }

        [Export] public bool CanDoubleJump { get; set; }

        [Node] public RespawnHelper Respawn { get; private set; } = default;

        public bool IgnoreDiePanic { get; set; }

        public Axes Axes => new Axes(Transform.basis, FacingDirection.Sign());

        public Vector3 Size => this.ShapeOwnerGetExtents(0);

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

        public PhysicsBody Floor { get; set; }

        public CollisionInfo Wall { get; set; }

        public CollisionInfo Ceiling { get; set; }

        public ActionType Action
        {
            get => _action;
            set
            {
                if (_action != value)
                {
                    LastAction = _action;
                    _action = value;
                    ChangeAnimation(value);
                }
            }
        }

        public ActionType LastAction { get; private set; }

        public ActionType NextAction { get; set; }

        public CollisionObject HeldBody { get; set; }

        public Pickup CarriedBody { get; set; }

        public Pickup PushedBody { get; set; }

        public Direction FacingDirection { get; set; }

        public ChangeLevelArea ChangeArea { get; set; }

        public Spatial CollectedTreasure { get; set; }

        public float AirTime { get; set; }

        public bool InBackground { get; set; }

        public float BlinkSpeed { get; set; } = 0f;

        public float Opacity { get; set; } = 1f;

        public float Brightness { get; set; }

        public bool Alive => Action.IsAlive();

        public bool OnFloor => Floor != null || _isOnFloor;

        public bool OnWall => Wall.Collider != null && _isOnWall;

        public bool OnCeiling => Ceiling.Collider != null && _isOnCeiling;

        public bool IsSwimming => Action.IsSwimming();

        public bool IsClimbing => Action.IsClimbing();

        public bool IsSliding => Action == ActionType.Slide || Action == ActionType.Land;

        [Node] public ActionSignals Signals { get; private set; } = default;

        [CameraHelper(typeof(GameCamera))] protected GameCamera GameCamera;

        [RootNode] protected MusicManager MusicManager;
        [RootNode] protected DebugManager DebugManager;
        [RootNode] protected DebuggingBag DebuggingBag;
        [RootNode] protected GameStateManager GameState;
        [RootNode] protected TimeManager TimeManager;

        [Node] protected Spatial Treasure;
        [Node] protected Sprite3D Sprite;
        [Node] protected AudioStreamPlayer AudioPlayer;
        [Node] protected AudioStreamPlayer StepsPlayer;
        [Node] protected AudioStreamPlayer SurfacePlayer;
        [Node] protected AnimationPlayer AnimationPlayer;
        [Node] protected CollisionShape Collision;
        [Node] protected Spatial CarryOffset;
        [Node] protected CubeAssembly CubeAssembly;

        private Vector3 _originalSize;
        private bool _isOnFloor, _isOnWall, _isOnCeiling;
        private bool _isLeftStep;
        private ActionType _action = ActionType.Idle;
        private Tuple<AudioStream, AudioStream> _footSteps;
        private Dictionary<SurfaceType, List<AudioStream>> _surfaceHits;

        public Gomez()
        {
            LoadSurfaceSounds();
        }

        public override void _Ready()
        {
            this.InjectNodes();
            this.RegisterCommands();

            GameCamera.Connect(nameof(BaseCamera.Rotating), this, nameof(OnRotating));
            GameCamera.Connect(nameof(BaseCamera.Rotated), this, nameof(OnRotated));

            _originalSize = Size;
            FacingDirection = Direction.Right;
        }

        private void LoadSurfaceSounds()
        {
            _footSteps = new Tuple<AudioStream, AudioStream>(
                GD.Load<AudioStream>("res://assets/Sounds/Gomez/Footsteps/left.wav"),
                GD.Load<AudioStream>("res://assets/Sounds/Gomez/Footsteps/right.wav")
            );

            _surfaceHits = new Dictionary<SurfaceType, List<AudioStream>>();
            foreach (string key in Enum.GetNames(typeof(SurfaceType)))
            {
                var list = new List<AudioStream>();
                var dir = new Directory();
                string path = $"res://assets/Sounds/Gomez/Footsteps/{key}/";

                if (dir.Open(path) == Error.Ok)
                {
                    dir.ListDirBegin(true, true);
                    while (true)
                    {
                        string file = dir.GetNext();
                        if (file == string.Empty)
                            break;
                        else if (!file.Contains(".import"))
                            list.Add(GD.Load<AudioStream>(path + file));
                    }

                    dir.ListDirEnd();
                }

                _surfaceHits.Add((SurfaceType) Enum.Parse(typeof(SurfaceType), key), list);
            }
        }

        public void PlaySurfaceHit(bool withStep)
        {
            if (Floor is null || Floor.GetGroups().Count <= 0) return;

            string[] groups = Floor.GetGroups().Cast<string>().ToArray();
            string[] surfaces = Enum.GetNames(typeof(SurfaceType));

            SurfaceType surface;
            string group = groups.FirstOrDefault(g => g.In(surfaces));
            Enum.TryParse(group, out surface);

            if (withStep)
            {
                StepsPlayer.VolumeDb = GD.Linear2Db(Random.Range(0.9f, 1.0f));
                StepsPlayer.PitchScale = Random.Range(0.8f, 1.2f);
                StepsPlayer.Stream = (_isLeftStep ? _footSteps.Item1 : _footSteps.Item2);
            }

            if (surface != SurfaceType.None)
            {
                SurfacePlayer.VolumeDb = GD.Linear2Db(Random.Range(0.9f, 1.0f));
                SurfacePlayer.PitchScale = Random.Range(0.8f, 1.2f);
                SurfacePlayer.Stream = _surfaceHits[surface].Choose();
            }

            _isLeftStep = !_isLeftStep;
            StepsPlayer.Play();
            SurfacePlayer.Play();
        }

        private void ChangeAnimation(ActionType type)
        {
            string name = type.GetAnimationName();

            if (string.IsNullOrEmpty(name))
                throw new NullReferenceException($"The animation `{name}` doesn't mapped in the .json file");

            if (!AnimationPlayer.HasAnimation(name))
                throw new InvalidOperationException($"AnimationPlayer doesn't contain `{name}`");

            if (AnimationPlayer.GetAnimation(name).Loop)
                AnimationPlayer.Stop();

            AnimationPlayer.Play(name);
            AnimationPlayer.PlaybackSpeed = 1f;
            AnimationPlayer.Advance(0);
        }

        public void Reset()
        {
            Floor = null;
            InBackground = false;
            Scale = Vector3.One;
            Action = ActionType.Idle;
            Opacity = 1f;
            Brightness = 1f;
        }

        public override void _PhysicsProcess(float delta)
        {
            Vector3 sizeScale = Vector3.One;
            if (CarriedBody != null && !CarriedBody.EnableCollision)
            {
                sizeScale = new Vector3(1.5f, 1f, 1.5f);
                if (Action != ActionType.ThrowTrile || Action != ActionType.ThrowHeavy)
                {
                    Vector3 offset = CarryOffset.Translation;
                    offset.x *= FacingDirection.Sign();
                    CarriedBody.Origin = ToGlobal(offset);
                }
            }

            this.ShapeOwnerSetExtents(0, _originalSize * sizeScale);
            IgnoreDiePanic = (_isOnFloor) ? false : IgnoreDiePanic;
            Respawn.Used = false;
            Respawn.Record();
            
            if (Action != ActionType.OpenTreasure)
                CheckForAreas();
            
            if (Action.AllowsDirectionChange() && !Mathf.IsZeroApprox(FzInput.Movement.x))
                FacingDirection = FzInput.Movement.x.GetDirectionFrom();
            Sprite.Scale = new Vector3(FacingDirection.Sign(), 1f, 1f);

            // Testing the waters before moving the Gomez
            Vector3? floorDistance = PhysicsHelper.TestAndCollide(this, OnFloor, delta);

            // Process responses from testing
            GlobalVelocity = PhysicsHelper.HugResponse(this);
            if (!Action.IsOnLedge() && !Action.IsClimbing() && !InBackground)
            {
                GlobalTransform = PhysicsHelper.TryCorrectDepth(this, floorDistance);
            }

            if (!OnFloor && Floor != null)
            {
                _isOnFloor = true;
                GlobalTransform = PhysicsHelper.TryCorrectDepth(this, floorDistance);
            }

            // The actual moving routine
            Vector3 snap = (Floor != null) ? PhysicsHelper.Snap * Axes.Up : Vector3.Zero;
            GlobalVelocity = MoveAndSlideWithSnap(GlobalVelocity, snap, Axes.Up);

            // Update collision info
            _isOnFloor = IsOnFloor();
            _isOnWall = IsOnWall();
            _isOnCeiling = IsOnCeiling();
        }

        public override void _Process(float delta)
        {
            Sprite.Opacity = (Action == ActionType.Hurt || Action == ActionType.Sink)
                ? Mathz.Clamp01(
                    (Mathf.Sin(BlinkSpeed * Mathf.Pi * 10f) + 0.5f - BlinkSpeed * 1.25f) * 2.0f)
                : 1f;
        }

        // ReSharper disable once UnusedParameter.Local
        private void OnRotating(float step)
        {
            GetTree().Paused = true;
            if (Action.IsOnLedge() || Action.IsClimbing())
                AnimationPlayer.PauseMode = PauseModeEnum.Process;

            Transform transform = GlobalTransform;
            transform.basis = GameCamera.GlobalTransform.basis;
            GlobalTransform = transform;
        }

        private void OnRotated()
        {
            GetTree().Paused = false;
            if (Action.IsOnLedge() || Action.IsClimbing())
                AnimationPlayer.PauseMode = PauseModeEnum.Inherit;

            Transform transform = GlobalTransform;
            transform.basis = GameCamera.Orthogonal.GetBasis();
            GlobalTransform = transform;
        }

        private void CheckForAreas()
        {
            var spaceState = GetWorld().DirectSpaceState;
            var detected = spaceState.CastRect<Area>(GlobalTransform, Size, PhysicsLayer.Actors);
            
            switch (detected.First)
            {
                case ChangeLevelArea change:
                    ChangeArea = change;
                    break;
                case Collectable collectable:
                    switch (collectable.ItemType)
                    {
                        case Collectable.Type.SmallCube:
                            Transform transform = collectable.GlobalTransform;
                            transform.basis = GlobalTransform.basis;
                            CubeAssembly.CollectCube(transform);
                            GameState.MakeNodeInactive(collectable, true);
                            break;

                        case Collectable.Type.BigCube:
                        case Collectable.Type.AntiCube:
                            CollectedTreasure = collectable;
                            GameState.MakeNodeInactive(collectable);
                            break;
                    }
                    break;
                default:
                    ChangeArea = null;
                    break;
            }
        }
    }
}