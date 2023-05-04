using Godot;
using Coroutine;
using System;
using System.Collections.Generic;
using Zeffyr.Managers;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Tools;
using Random = Zeffyr.Tools.Random;

namespace Zeffyr.Components
{
    public class GameCamera : BaseCamera
    {
        [Export] public bool CanFollow { get; set; } = true;

        [Export] public bool CanRotate { get; set; } = true;

        [Export] public NodePath TargetPath { get; set; }

        [Export(PropertyHint.Range, "0,16,1")]
        public float DragHorizontal = 2f;

        [Export(PropertyHint.Range, "0,16,1")]
        public float DragVertical;

        [Export(PropertyHint.Range, "0.1,1.0,0.1")]
        public float DragDamp = 0.2f;

        [Export(PropertyHint.Range, "0.1,1.0,0.1")]
        public float RotateTime = 0.45f;

        [Export(PropertyHint.Range, "0.1,1.0,0.1")]
        public float OffsetSpeed = 0.1f;

        [Node] protected Timer Timer;

        [Node] protected AudioStreamPlayer SwooshLeft;

        [Node] protected AudioStreamPlayer SwooshRight;

        [RootNode] protected DebuggingBag DebuggingBag;

        [RootNode] protected FadeManager FadeManager;

        public Orthogonal LastOrthogonal { get; protected set; }
        
        public Orthogonal Orthogonal
        {
            get => _orthogonal;
            set
            {
                LastOrthogonal = _orthogonal;
                _orthogonal = value;
            }
        }

        private Orthogonal _orthogonal;
        private Spatial _target;
        private bool _panning;

        public override void _Ready()
        {
            base._Ready();
            this.InjectNodes();

            Orthogonal = Transform.basis.GetOrthogonal();
            if (TargetPath.IsEmpty())
                throw new NullReferenceException($"Target node path is empty in {GetParent().Name}/{Name}");
            else
                _target = this.GetNodeOrNull<Spatial>(TargetPath);
        }

        public override void _Process(float delta)
        {
            Func<IEnumerator<Wait>> action = () =>
            {
                Vector3 to = Vector3.Up * Mathf.Deg2Rad(90f);
                
                if (FzInput.IsJustPressed(InputAction.RotateLeft) && CanRotate)
                {
                    SwooshLeft.Play();
                    Orthogonal = Orthogonal.Rotated(-1);
                    return RotateAround(Rotation, Rotation - to, RotateTime);
                }

                if (FzInput.IsJustPressed(InputAction.RotateRight) && CanRotate)
                {
                    SwooshRight.Play();
                    Orthogonal = Orthogonal.Rotated(1);
                    return RotateAround(Rotation, Rotation + to, RotateTime);
                }

                _panning = PanByInput(FzInput.FreeLook, OffsetSpeed, Size / 2f);
                if (CanFollow && !_panning)
                {
                    FollowTarget();
                }
                return null;
            };
            this._Tick(delta, action);
        }

        public void ChangeRotation(Orthogonal nextOrthogonal, float speedFactor = 1f)
        {
            Vector3 from = Orthogonal.GetBasis().GetEuler();
            Vector3 to = nextOrthogonal.GetBasis().GetEuler();
            Orthogonal = nextOrthogonal;
            this._Call(RotateAround(from, to, RotateTime * speedFactor));
        }

        private void FollowTarget()
        {
            Vector2 screenSize = GetViewport().Size;
            Vector3 camera = Transform.basis.XformInv(Origin);
            Vector3 target = Transform.basis.XformInv(_target.Transform.origin);
            Vector3 center = Transform.basis.XformInv(ProjectRayOrigin(screenSize / 2f - Offset));

            Vector2 half = new Vector2(DragHorizontal, DragVertical);
            Vector3 diff = target - center;
            Vector3 next = camera;
            
            if (diff.x > half.x)
                next.x = target.x - (half.x + Offset.x);
            else if (diff.x < -half.x)
                next.x = target.x + (half.x + Offset.x);

            if (diff.y > half.y)
                next.y = target.y - (half.y + Offset.y);
            else if (diff.y < -half.y)
                next.y = target.y + (half.y + Offset.y);

            camera.x = Mathz.SmoothStep(camera.x, next.x, DragDamp);
            camera.y = Mathz.SmoothStep(camera.y, next.y, DragDamp);

            camera.x = Mathf.RoundToInt(camera.x * PixelsPerUnit) / PixelsPerUnit;
            camera.y = Mathf.RoundToInt(camera.y * PixelsPerUnit) / PixelsPerUnit;
            camera.z = target.z;
            
            Origin = Transform.basis.Xform(camera);
        }

        private IEnumerator<Wait> _Shake(float distance, float duration)
        {
            float remain, elapsed = 0f;
            Vector2 initial = Offset;
            
            FzInput.Joypad.Vibrate(Joypad.Motors.RightWeak, 1f, duration);
            FzInput.Joypad.Vibrate(Joypad.Motors.LeftStrong, 1f, duration);

            while (elapsed <= duration)
            {
                elapsed += GetProcessDeltaTime();
                remain = Mathf.Lerp(distance, 0f, Mathf.Sqrt(elapsed / duration));

                Vector2 noise = new Vector2()
                {
                    x = Random.Centered(remain * 2f),
                    y = Random.Centered(remain * 2f),
                };

                Offset = initial + noise;
                yield return new Wait();
            }
            
            Offset = initial;
        }

        public void Shake(float distance, float duration)
        {
            this._Call(_Shake(distance, duration));
        }

        public void Flash(Color color, float duration)
        {
            FadeManager.DoFade(color, Colors.Transparent, duration);
        }
    }
}