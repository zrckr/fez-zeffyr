using Godot;
using Zeffyr.Managers;
using Zeffyr.Structure;

namespace Zeffyr.Components.Actions
{
    public abstract class PlayerAction : Node
    {
        private bool _wasActive;
        private bool _enabled = true;

        [Export]
        public bool Enabled
        {
            get => _enabled;
            set
            {
                SetProcess(value);
                _enabled = value;
            }
        }

        [Node("../..")]
        protected readonly IPlayerEntity Player = default;

        [RootNode("DebugManager")]
        protected readonly DebugManager Debug = default;

        [Node("../../Signals")]
        protected readonly ActionSignals Signals = default;

        [Node("../../AnimationPlayer")]
        protected readonly AnimationPlayer AnimationPlayer = default;

        [Node("../../AudioPlayer")]
        protected readonly AudioStreamPlayer AudioPlayer = default;

        [Node("../../Tween")]
        protected readonly Tween Tween = default;

        [RootNode("GameState")]
        protected readonly GameStateManager GameState = default;

        [RootNode("FadeManager")]
        protected readonly FadeManager FadeManager = default;

        [RootNode("MusicManager")]
        protected readonly MusicManager MusicManager = default;

        [Node("../WalkTo")]
        protected readonly WalkTo WalkTo = default;

        [CameraHelper(typeof(GameCamera))]
        protected readonly GameCamera GameCamera = default;

        protected Level Level => (Level) GetTree().CurrentScene;

        protected PhysicsDirectSpaceState SpaceState { get; private set; }

        protected virtual void TransitionAttempts() { }

        protected virtual void OnEnter() { }

        protected virtual void OnEnd() { }

        protected virtual void OnAct(float delta) { }

        protected abstract bool IsAllowed(ActionType type);

        public override void _Ready()
        {
            this.InjectNodes();
            this.SetPhysicsProcess(Enabled);
        }

        public void Reset() => this._Ready();

        public override void _PhysicsProcess(float delta)
        {
            SpaceState = Player.GetWorld().DirectSpaceState;
            TransitionAttempts();
            bool isActive = IsAllowed(Player.Action);
            if (isActive)
            {
                if (!_wasActive) OnEnter();
                else OnAct(delta);
            }
            else if (_wasActive) OnEnd();
            _wasActive = isActive;
        }
    }
}