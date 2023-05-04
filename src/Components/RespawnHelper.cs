using Godot;
using Zeffyr.Managers;
using Zeffyr.Structure;
using Zeffyr.Structure.Physics;

namespace Zeffyr.Components
{
    public class RespawnHelper : Node
    {
        public bool Used { get; set; }
            
        public ActionType LastAction { get; private set; }

        public Vector3 RespawnOrigin { get; private set; }

        public Orthogonal Orthogonal { get; private set; }

        public Direction LastFacingDirection { get; private set; }

        public Vector3 FloorOriginOnLeave { get; private set; }

        public PhysicsBody LastFloorEntity { get; private set; }

        public CollisionObject LastHeldBody { get; private set; }

        public float OffsetOnFloorLeave { get; private set; }

        public PhysicsBody Checkpoint { get; set; }
        
        [Node("..")]
        protected IPlayerEntity Player;
        
        [RootNode("GameState")]
        protected GameStateManager GameState;
        
        [CameraHelper(typeof(GameCamera))]
        protected GameCamera GameCamera;
        
        [RootNode("TimeManager")]
        protected TimeManager TimeManager;

        public override void _Ready()
        {
            this.InjectNodes();
        }

        public void Load()
        {
            Used = true;
            Player.Origin = RespawnOrigin;
            Player.Action = (LastAction == ActionType.None) ? ActionType.Idle : LastAction;
            Player.FacingDirection = LastFacingDirection;
            Player.Velocity = Player.Axes.Up * PhysicsHelper.Snap;
            Player.HeldBody = LastHeldBody;
            Player.Floor = LastFloorEntity;
            GameCamera.ChangeRotation(Orthogonal);
        }

        public void LoadAtCheckpoint()
        {
            Player.HeldBody = null;
            Player.CarriedBody = null;
            GameCamera.ChangeRotation(GameState.SaveData.View, 0f);

            if (Checkpoint is null)
            {
                Vector3 floor = new Vector3()
                {
                    x = GameState.SaveData.Floor[0],
                    y = GameState.SaveData.Floor[1],
                    z = GameState.SaveData.Floor[2],
                };
                Player.Origin = floor + Player.Axes.Up * Player.Size.y * 2f;
            }
            else
            {
                Vector3 extents = Checkpoint.ShapeOwnerGetXFormExtents(0, Player.GlobalTransform.basis);
                Vector3 origin = Checkpoint.GlobalTransform.origin;
                Player.Origin = origin + Player.Axes.Up * (extents.y + Player.Size.y);
            }

            Player.Action = ActionType.Idle;
            Player.FacingDirection = Direction.Right;
        }

        public void Record(bool markCheckpoint = false)
        {
            if (Player.OnFloor || Player.IsClimbing || Player.IsSwimming ||
                LastAction == ActionType.CornerGrab ||
                LastAction == ActionType.EnterPipe)
            {
                Axes axes = Player.Axes;
                Basis basis = Player.Transform.basis;
                Orthogonal orthogonal = Player.Transform.basis.GetOrthogonal();

                if (Player.IsClimbing)
                {
                    FloorOriginOnLeave =
                        axes.XMask * Player.Origin.Floor() + axes.Right * 0.5f +
                        (axes.YMask * Player.Origin).Ceil() + axes.Up * 0.5f +
                        axes.ZMask * Player.Origin.z;
                }
                else if (LastAction == ActionType.CornerGrab || LastAction.IsSwimming() || LastAction == ActionType.EnterPipe)
                {
                    FloorOriginOnLeave = Player.Origin;
                }
                else if (LastFloorEntity != null)
                {
                    Vector3 extents = LastFloorEntity.ShapeOwnerGetXFormExtents(0, basis);
                    FloorOriginOnLeave = LastFloorEntity.GlobalTransform.origin + axes.Up * (extents.y + Player.Size.y);
                }

                OffsetOnFloorLeave = GameCamera.Offset.y;
                if (!Player.Action.DisallowsRespawn() && !Player.InBackground)
                {
                    LastAction = Player.Action;
                    RespawnOrigin = FloorOriginOnLeave;
                    LastFacingDirection = Player.FacingDirection;
                    LastFloorEntity = Player.Floor;
                    LastHeldBody = Player.HeldBody;
                    Orthogonal = orthogonal;
                }

                if (markCheckpoint)
                {
                    Checkpoint = LastFloorEntity;
                    GameState.SaveData.Level = GetTree().CurrentScene.Filename;
                    GameState.SaveData.View = orthogonal;
                    GameState.SaveData.TimeOfDay = TimeManager.CurrentTime.TimeOfDay.Ticks;
                    GameState.SaveData.Floor = new []
                    {
                        LastFloorEntity!.GlobalTransform.origin.x,
                        LastFloorEntity!.GlobalTransform.origin.y,
                        LastFloorEntity!.GlobalTransform.origin.z,
                    };
                }
            }
        }
    }
}