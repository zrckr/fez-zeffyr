using Godot;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Tools;

namespace Zeffyr.Components.Actions
{
    public class Idle : PlayerAction
    {
        [Export] public float MinWait = 8f;
        [Export] public float MaxWait = 10f;
        
        [Node] private Timer Timer = null;
        [Node] private Timer Timer2 = null;

        private readonly ActionType[] _sequence = new ActionType[]
        {
            ActionType.IdleSleep,
            ActionType.IdlePlay,
            ActionType.IdleLookAround,
            ActionType.IdleYawn
        };
        
        protected override void TransitionAttempts()
        {
            switch (Player.Action)
            {
                case ActionType.Walk:
                case ActionType.Run:
                case ActionType.DropDown:
                case ActionType.Slide:
                case ActionType.CarryWalk:
                case ActionType.CarryHeavyWalk:
                    if (Mathf.IsZeroApprox(Player.Velocity.x) && Player.Velocity.y <= 0.0f &&
                        Mathf.IsZeroApprox(FzInput.Movement.x))
                    {
                        bool? anyCarried = (Player.CarriedBody is null) ? new bool?() : Player.CarriedBody.IsHeavy;
                        Player.Action = anyCarried switch
                        {
                            false => ActionType.CarryIdle,
                            true => ActionType.CarryHeavyIdle,
                            null => ActionType.Idle,
                        };
                    }
                    break;
            }
        }

        private void ScheduleNextIdle()
        {
            Player.Action = ActionType.Idle;
            Timer.Stop();
            Timer.WaitTime = Random.Range(MinWait, MaxWait);
            Timer.Start();
        }

        private void ChooseRandomIdle()
        {
            ActionType nextIdle = _sequence.Choose();
            string nextName = nextIdle.GetAnimationName();
            
            Player.Action = nextIdle;
            Timer2.WaitTime = AnimationPlayer.GetAnimation(nextName).Length;
            Timer2.Start();
        }

        protected override void OnEnter()
        {
            this.InjectNodes();

            Timer.Connect("timeout", this, nameof(ChooseRandomIdle));
            Timer2.Connect("timeout", this, nameof(ScheduleNextIdle));
            
            if (Player.CarriedBody is null)
                ScheduleNextIdle();
        }
        
        protected override void OnEnd()
        {
            Timer.Stop();
            Timer2.Stop();
            Timer.Disconnect("timeout", this, nameof(ChooseRandomIdle));
            Timer2.Disconnect("timeout", this, nameof(ScheduleNextIdle));
        }

        protected override bool IsAllowed(ActionType type) =>
            type.In(ActionType.Idle, ActionType.IdleSleep, ActionType.IdlePlay, ActionType.IdleLookAround,
                ActionType.IdleYawn, ActionType.CarryIdle, ActionType.CarryHeavyIdle);
    }
}