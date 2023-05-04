using Godot;
using Zeffyr.Structure;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class Sink : PlayerAction
    {
        [Export] public AudioStreamSample BurnSound;
        [Export] public AudioStreamSample DrownSound;

        private bool _doneFor;
        private float _elapsed;
        
        protected override void OnEnter()
        {
            Player.CarriedBody.EnableCollision = true;
            Player.CarriedBody = null;
            Player.Velocity = new Vector3(0f, -0.005f, 0f);

            if (Level.Water.Type == LiquidType.Lava)
                AudioPlayer.Stream = BurnSound;
            else if (Level.Water.Type == LiquidType.Sewer)
                AudioPlayer.Stream = DrownSound;
            AudioPlayer.Play();
            
            _elapsed = 0f;
            _doneFor = Player.Respawn.RespawnOrigin.y < Level.Water.Height - 0.25f;
        }

        protected override void OnAct(float delta)
        {
            float end = _doneFor ? 1.25f : 2f;
            _elapsed += delta;
            if (_elapsed > end)
            {
                if (_doneFor)
                {
                    FadeManager.DoFade(Colors.Transparent, Colors.Black, 1f);
                    FadeManager.Faded += DoRespawn;
                }
                else
                {
                    Player.Respawn.Load();
                }
            }
            else
            {
                Player.BlinkSpeed = Easing.In.Cubic(_elapsed / end) * 1.5f;
            }
        }

        private void DoRespawn()
        {
            FadeManager.DoFade(Colors.Black, Colors.Transparent, 1.5f);
            GameState.LoadGame();
            GameState.ChangeLevel(Level.Name);
            Player.Respawn.LoadAtCheckpoint();
        }

        protected override bool IsAllowed(ActionType type) => type == ActionType.Sink;
    }
}