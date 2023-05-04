using Godot;
using Zeffyr.Managers;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;

namespace Zeffyr.Components.Actions
{
    public class OpenDoor : PlayerAction
    {
        [Export] public readonly float OpeningDuration = 1.25f;

        [Export] public readonly AudioStreamSample UnlockSound;

        [Export] public readonly AudioStreamSample OpenSound;

        protected override void TransitionAttempts()
        {
            switch (Player.Action)
            {
                case ActionType.Idle:
                case ActionType.IdlePlay:
                case ActionType.IdleSleep:
                case ActionType.IdleLookAround:
                case ActionType.IdleYawn:
                case ActionType.LookLeft:
                case ActionType.LookRight:
                case ActionType.LookUp:
                case ActionType.LookDown:
                case ActionType.Walk:
                case ActionType.Run:
                case ActionType.Slide:
                case ActionType.Land:
                case ActionType.Teeter:
                case ActionType.TeeterPaul:
                    if (FzInput.IsJustPressed(InputAction.Up) && Player.OnFloor &&
                        Player.ChangeArea?.Door != null && !Player.InBackground)
                    {
                        if (Player.ChangeArea.KeyRequired)
                            if (GameState.SaveData.Keys <= 0)
                                break;

                        WalkTo.NextOrigin = GetDoorOrigin;
                        WalkTo.NextAction = ActionType.OpenDoor;
                        Player.Action = ActionType.WalkTo;
                    }

                    break;
            }
        }

        private Vector3 GetDoorOrigin()
        {
            return Player.Origin * Player.Axes.YzMask +
                   Player.ChangeArea.GlobalTransform.origin * Player.Axes.XMask;
        }

        protected override void OnEnter()
        {
            this.InjectNodes();
            Player.Velocity *= Vector3.Up;
            Player.FacingDirection = Direction.Right;
            
            if (Player.ChangeArea.KeyRequired)
            {
                GameState.SaveData.ThisLevel.FilledConditions.LockedDoors += 1;
                GameState.SaveData.Keys -= 1;
                GameState.EmitSignal(nameof(GameStateManager.OnHudChanged));
            }
            else
            {
                GameState.SaveData.ThisLevel.FilledConditions.UnlockedDoors += 1;
            }

            GameState.MakeNodeInactive(Player.ChangeArea.Door);

            Vector3 initial = Player.ChangeArea.Door.Rotation;
            Vector3 final = initial + Vector3.Up * Mathf.Deg2Rad(90f);
            Tween.InterpolateProperty(Player.ChangeArea.Door, "rotation", initial, final, OpeningDuration,
                Tween.TransitionType.Sine);
            Tween.InterpolateProperty(Player.ChangeArea.Door, "scale:z", 1f, 0f, OpeningDuration,
                Tween.TransitionType.Sine);
        }

        protected override void OnAct(float delta)
        {
            if (Mathf.IsEqualApprox(AnimationPlayer.CurrentAnimationPosition, 0.7f, delta))
            {
                AudioPlayer.Stream = (Player.ChangeArea.KeyRequired) ? UnlockSound : OpenSound;
                AudioPlayer.Play();
                Tween.Start();
            }

            if (!AnimationPlayer.IsPlaying())
            {
                Player.ChangeArea.Door.Visible = false;
                Player.ChangeArea.Door.QueueFree();
                Player.Action = ActionType.Idle;
            }
        }

        protected override bool IsAllowed(ActionType type) => type == ActionType.OpenDoor;
    }
}