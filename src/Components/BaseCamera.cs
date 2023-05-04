using Godot;
using Coroutine;
using System;
using System.Collections.Generic;
using Zeffyr.Tools;

namespace Zeffyr.Components
{
    public class BaseCamera : Spatial
    {
        [Export(PropertyHint.Range, "1,5,1")]
        public int PixelsPerTrixel { get; protected set; } = 1;

        [Signal] public delegate void PreRotate();

        [Signal] public delegate void Rotating(float step);

        [Signal] public delegate void Rotated();

        [Signal] public delegate void PreZoom();

        [Signal] public delegate void Zooming(float step);

        [Signal] public delegate void Zoomed();

        public Vector3 Origin
        {
            get => GlobalTransform.origin;
            set
            {
                Transform transform = GlobalTransform;
                transform.origin = value;
                GlobalTransform = transform;
            }
        }

        public bool Current
        {
            get => _camera.Current;
            set => _camera.Current = value;
        }
        
        public float Size
        {
            get => _camera.Size;
            set => _camera.Size = value;
        }
        
        public Vector2 Offset
        {
            get => new Vector2(_camera.HOffset, _camera.VOffset);
            set
            {
                _camera.HOffset = value.x;
                _camera.VOffset = value.y;
            }
        }

        public Transform CameraTransform
        {
            get => _camera.Transform;
            set => _camera.Transform = value;
        }

        public Viewport Viewport => _camera.GetViewport();

        public float Aspect => Viewport.Size.x / Viewport.Size.y;

        public Tween Tween { get; protected set; }
        
        public float PixelsPerUnit { get; protected set; }

        public void MakeCurrent() => _camera.MakeCurrent();

        public Vector3 ProjectRayOrigin(Vector2 screenPoint) => _camera.ProjectRayOrigin(screenPoint);

        public Vector3 ProjectRayNormal(Vector2 screenPoint) => _camera.ProjectRayNormal(screenPoint);

        public Vector2 UnprojectPosition(Vector3 worldPoint) => _camera.UnprojectPosition(worldPoint);
        
        private Camera _camera;
        private CoroutineHandlerInstance _handler;
        private ActiveCoroutine _activeFunc;

        public BaseCamera()
        {
            _handler = new CoroutineHandlerInstance();
            _activeFunc = _handler.Initialize();
        }

        public override void _Ready()
        {
            _camera = GetNodeOrNull<Camera>("Camera");
            Tween = GetNodeOrNull<Tween>("Tween");
            
            PixelsPerUnit = PixelsPerTrixel * Mathz.TrixelsPerUnit;
            Size = Viewport.Size.y / PixelsPerUnit;
        }

        protected IEnumerator<Wait> RotateAround(Vector3 from, Vector3 to, float time)
        {
            if (from.IsEqualApprox(to))
                yield break;
            float elapsed = 0f;
            
            Tween.InterpolateProperty(this, "rotation",
                from, to, time,
                Tween.TransitionType.Linear, Tween.EaseType.OutIn);

            Tween.Start();
            EmitSignal(nameof(PreRotate));

            while (Tween.IsActive())
            {
                elapsed += GetProcessDeltaTime();
                EmitSignal(nameof(Rotating), elapsed / time);
                yield return new Wait();
            }
            
            EmitSignal(nameof(Rotated));
        }

        protected IEnumerator<Wait> ZoomingSize(float from, float to, float time, bool backwards = false)
        {
            var ease = (backwards) ? Tween.EaseType.In : Tween.EaseType.Out;
            float elapsed = 0f;
            
            if (backwards)
            {
                Tween.InterpolateProperty(this, nameof(Size), to, from, time,
                    Tween.TransitionType.Linear, ease);
            }
            else
            {
                Tween.InterpolateProperty(this, nameof(Size), from, to, time,
                    Tween.TransitionType.Linear, ease);
            }

            Tween.Start();

            EmitSignal(nameof(PreZoom));
            while (Tween.IsActive())
            {
                elapsed += GetProcessDeltaTime();
                EmitSignal(nameof(Zooming), elapsed / time);
                yield return new Wait();
            }

            EmitSignal(nameof(Zoomed));
        }

        protected bool TiltByInput(Vector3 @default, Vector2 input, Vector2 angles, float time)
        {
            Vector3 rot = Rotation;
            rot.x = Mathz.SmoothStep(rot.x, @default.x + input.y * angles.y, time);
            rot.y = Mathz.SmoothStep(rot.y, @default.y + input.x * -angles.x, time);
            Rotation = rot;

            return !Rotation.IsEqualApprox(@default);
        }

        protected bool PanByInput(Vector2 input, float time, float limit)
        {
            Vector2 offset = Offset;
            if (input.IsZeroApprox())
            {
                offset.x = Mathf.MoveToward(offset.x, 0f, time);
                offset.y = Mathf.MoveToward(offset.y, 0f, time);
            }
            else
            {
                Vector2 next = input * limit;
                offset.x = Mathf.Lerp(offset.x, next.x, time);
                offset.y = Mathf.Lerp(offset.y, next.y, time);
            }

            offset.x = Mathf.RoundToInt(offset.x * PixelsPerUnit) / PixelsPerUnit;
            offset.y = Mathf.RoundToInt(offset.y * PixelsPerUnit) / PixelsPerUnit;

            Offset = offset;
            return !Mathf.IsZeroApprox(offset.x) || !Mathf.IsZeroApprox(offset.y);
        }

        protected void _Call(IEnumerator<Wait> function)
        {
            if (!_activeFunc.IsFinished)
            {
                _activeFunc.Cancel();
                Tween.StopAll();
            }

            if (function != null)
                _activeFunc = _handler.Start(function);
            else
                _activeFunc = _handler.Initialize();
        }

        protected void _Tick(float delta, Func<IEnumerator<Wait>> conditions = null)
        {
            if (!_activeFunc.IsFinished)
            {
                _handler.Tick(delta);
                return;
            }
            IEnumerator<Wait> nextFunc = conditions?.Invoke();
            if (nextFunc != null)
                _activeFunc = _handler.Start(nextFunc);
            else
                _activeFunc = _handler.Initialize();
        }
    }
}