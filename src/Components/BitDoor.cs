using Godot;
using System;
using Zeffyr.Managers;
using Zeffyr.Structure;
using Zeffyr.Tools;
using Random = Zeffyr.Tools.Random;

namespace Zeffyr.Components
{
    public class BitDoor : Spatial
    {
        [Export]
        public readonly int BitCount;
        
        [Export]
        public readonly float ShakeTime = 0.5f;

        [Export]
        public readonly float OpenTime = 3.0f;

        [RootNode]
        private GameStateManager GameState { get; set; } = default;

        [Node]
        private AudioStreamPlayer Audio { get; set; } = default;

        private Level CurrentLevel => GetTree().CurrentScene as Level;

        private Axes Axes => new Axes(GlobalTransform.basis);

        private Vector3 Origin
        {
            get => GlobalTransform.origin;
            set
            {
                Transform transform = GlobalTransform;
                transform.origin = value;
                GlobalTransform = transform;
            }
        }
        
        private bool _close;
        private bool _opening;
        private float _sinceMoving;
        private float _sinceClose;
        private Vector3 _doorStart;
        private Vector3 _doorEnd;

        public override void _Ready() => this.InjectNodes();

        public override void _Process(float delta)
        {
            DetermineIsClose();
            if (!_opening && !_close && _sinceClose > 0f)
                _sinceClose -= delta;
            else if (_close && _sinceClose < 3f)
                _sinceClose += delta;
            
            if (_sinceClose > 0.5f && DetermineInArea())
            {
                if (GameState.SaveData.BigCubes + GameState.SaveData.AntiCubes >= BitCount)
                    OpenDoor(delta);
                else
                    GameState.EmitSignal(nameof(GameStateManager.OnHudChanged));
            }
        }

        private void DetermineIsClose()
        {
            _close = false;
            if (Visible && DetermineInArea() && !CurrentLevel.Gomez.InBackground)
            {
                Basis basis = GlobalTransform.basis;
                Vector3 dir = Origin + basis.Xform(basis.Xform(Vector3.Back));
                Vector3 diff = (dir - Origin).Abs() * Axes.XyMask;
                _close = diff.x + diff.z < 2f && diff.y < 2f && (dir - Origin).Dot(Axes.Forward) >= 0f;
            }
        }

        private bool DetermineInArea()
        {
            ulong parentId = GetParent().GetInstanceId();
            ulong areaId = 0;
            if (CurrentLevel is not null && CurrentLevel.Gomez.ChangeArea is not null)
                areaId = CurrentLevel.Gomez.ChangeArea.GetInstanceId();
            return parentId == areaId;
        }

        private void OpenDoor(float delta)
        {
            if (!_opening)
            {
                _doorStart = Origin + GetOpenOffset() * Axes.XzMask;
                _doorEnd = Origin + GetOpenOffset();
                _opening = true;
                Audio.Play();
                CurrentLevel.ResolvePuzzle();
            }
            _sinceMoving += delta;
            
            Vector3 next = _doorStart;
            if (_sinceMoving > ShakeTime)
            {
                float remain = _sinceMoving - ShakeTime;
                float diff = remain / OpenTime;
                float step = Mathz.Clamp01(Easing.InOut.Sine(diff));
                next = _doorStart.LinearInterpolate(_doorEnd, step);
            }

            Vector3 shake = new Vector3()
            {
                x = Random.Centered(0.03f),
                y = Random.Centered(0.03f),
                z = Random.Centered(0.03f),
            };
            Origin = next + shake * Axes.XyMask;

            if (_sinceMoving > OpenTime + ShakeTime)
            {
                _opening = false;
                Visible = false;
                GameState.MakeNodeInactive(this, true);
                GameState.SaveGame();
            }
        }

        private Vector3 GetOpenOffset()
        {
            switch (BitCount)
            {
                case 1:
                case 0:
                case 4:
                case 8:
                case 16:
                    return Axes.Up * 4f - Axes.Forward * 0.125f;
                case 2:
                    return Axes.Up * 4f;
                case 32:
                    return Axes.Up * -4f - Axes.Forward *  3f / 16f;
                case 64:
                    return Axes.Up * -4f - Axes.Forward * 0.125f;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}