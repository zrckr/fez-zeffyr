using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Physics;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class Hurt : PlayerAction
    {
        private bool _doneFor;
        private bool _causedByActor;
        private float _elapsed;

        protected override void TransitionAttempts()
        {
            switch (Player.Action)
            {
                case ActionType.Dying:
                case ActionType.Hurt:
                case ActionType.SuckedIn:
                    break;
                default:
                    CollisionObject obj = SpaceState.CastRect(Player.GlobalTransform, Player.Size, 
                        PhysicsLayer.Deadly).First;
                    if (obj is not null)
                    {
                        Player.Action = ActionType.Hurt;
                        _causedByActor = true;
                        _doneFor = Player.Respawn.RespawnOrigin.y < Level.Water.Height - 0.25f;
                    }

                    break;
            }
        }

        protected override void OnEnter()
        {
            if (Player.HeldBody != null)
            {
                Player.HeldBody = null;
                Player.Action = ActionType.Idle;
                Player.Action = ActionType.Hurt;
            }

            Player.CarriedBody = null;
            Player.Velocity = _causedByActor
                ? Mathz.Trixel * (Vector3.Right * Player.FacingDirection.GetOpposite().Sign() + Vector3.Up)
                : Vector3.Zero;
        }

        protected override void OnAct(float delta)
        {
            float end = _doneFor ? 1.25f : 2f;
            if (_elapsed <= end)
            {
                _elapsed += delta;
                Player.BlinkSpeed = Easing.In.Cubic(_elapsed / 1.25f) * 1.5f;
            }
            else
            {
                _elapsed = 0f;
                _causedByActor = false;
                if (_doneFor)
                {
                    FadeManager.DoFade(Colors.Transparent, Colors.Black, 1f);
                    FadeManager.Faded += DoRespawn;
                }
                else
                {
                    Player.Action = ActionType.Idle;
                }
            }
        }

        private void DoRespawn()
        {
            FadeManager.DoFade(Colors.Black, Colors.Transparent, 1.5f);
            GameState.LoadGame();
            GameState.ChangeLevel(Level.Name);
            Player.Respawn.LoadAtCheckpoint();
        }

        protected override bool IsAllowed(ActionType type) => type == ActionType.Hurt;
    }
}