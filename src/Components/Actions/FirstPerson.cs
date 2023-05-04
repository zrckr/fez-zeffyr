using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;

namespace Zeffyr.Components.Actions
{
    public class FirstPerson : PlayerAction
    {
        [Export] public float MouseSensitivity = 0.05f;

        [Export] public float JoySensitivity = 2f;

        [Export] public float FieldOfView = 90.0f;
        
        [Export] public float Acceleration = 4.5f;
        
        [Export] public float Deceleration = 16f;

        [Node] protected Camera FpsCamera;

        private const float RotationLimit = 70f;
        private const float MaxSlopeAngle = 45f;
        private Vector3 _velocity;

        public override void _Ready()
        {
            base._Ready();
            this.InjectNodes();
            SetProcessInput(false);
        }

        protected override void TransitionAttempts()
        {
            switch (Player.Action)
            {
                case ActionType.IdlePlay:
                case ActionType.IdleSleep:
                case ActionType.IdleLookAround:
                case ActionType.IdleYawn:
                case ActionType.Idle:
                case ActionType.LookLeft:
                case ActionType.LookRight:
                case ActionType.LookUp:
                case ActionType.LookDown:
                case ActionType.Walk:
                case ActionType.Run:
                case ActionType.Slide:
                case ActionType.Land:
                case ActionType.Teeter:
                    if (FzInput.IsPressed(InputAction.FpsToggle))
                    {
                        Player.Action = ActionType.FirstPerson;
                    }
                    break;
            }
        }

        protected override void OnEnter()
        {
            GameCamera.Current = false;
            FpsCamera.Current = true;
            FpsCamera.Fov = FieldOfView;
            FzInput.Mouse.Mode = Input.MouseMode.Captured;
            Signals.EmitSignal(nameof(ActionSignals.EnterFirstPersonMode));
            
            SetProcessInput(true);
            Player.SetPhysicsProcess(false);
        }

        protected override void OnAct(float delta)
        {
            ProcessViewInput();
            ProcessMovement(delta);
        }

        protected override void OnEnd()
        {
            GameCamera.Current = true;
            FpsCamera.Current = false;
            Signals.EmitSignal(nameof(ActionSignals.ExitFirstPersonMode));
            FzInput.Mouse.Mode = Input.MouseMode.Visible;
            
            SetProcessInput(false);
            Player.SetPhysicsProcess(true);
        }

        private void ProcessViewInput()
        {
            if (FzInput.Event is InputEventJoypadMotion)
            {
                this.Call("rotate_x", Mathf.Deg2Rad(FzInput.Joypad.RightStick.y * -JoySensitivity));
                Player.Call("rotate_y", Mathf.Deg2Rad(FzInput.Joypad.RightStick.x * -JoySensitivity));
            } 

            Vector3 rot = (Vector3) this.Get("rotation_degrees");
            rot.x = Mathf.Clamp(rot.x, -RotationLimit, RotationLimit);
            this.Set("rotation_degrees", rot);
        }

        public override void _Input(InputEvent @event)
        {
            if (FzInput.Event is InputEventMouseMotion)
            {
                float xChange = FzInput.Mouse.Relative.y * -MouseSensitivity;
                float yChange = FzInput.Mouse.Relative.x * -MouseSensitivity;

                float xRot = ((Vector3) this.Get("rotation")).x;
                if (xChange > 0)
                    xChange = Mathf.Min(xChange, RotationLimit - xRot);
                else
                    xChange = Mathf.Max(xChange, -RotationLimit - xRot);
                
                this.Call("rotate_x", Mathf.Deg2Rad(xChange));
                Player.Call("rotate_y", Mathf.Deg2Rad(yChange));
            }
        }

        private void ProcessMovement(float delta)
        {
            KinematicBody kinematic = (KinematicBody) Player;
            Axes camera = new Axes(FpsCamera.GlobalTransform.basis);

            Vector3 dir = Vector3.Zero;
            dir -= camera.Forward * FzInput.Movement.y;
            dir += camera.Right * FzInput.Movement.x;
            dir *= Vector3.One - camera.Up;
            dir = dir.Normalized();
            
            Vector3 vh = _velocity * (Vector3.One - Player.Axes.Up);
            float accel = (dir.Dot(vh) > 0f) ? Acceleration : Deceleration;
            
            Vector3 target = dir * WalkRun.Default.WalkSpeed;
            Vector3 newVel = Vector3.Zero;
            newVel += vh.LinearInterpolate(target, delta * accel);
            newVel += delta * Jump.Default.Fall * Player.Axes.Up;

            _velocity = kinematic.MoveAndSlide(newVel, Player.Axes.Up,
                true, 4, Mathf.Deg2Rad(MaxSlopeAngle));
        }

        protected override bool IsAllowed(ActionType type) => type == ActionType.FirstPerson;
    }
}