using System;
using Godot;

using static Godot.Tween;

namespace Zeffyr.Managers
{
    public class FadeManager : CanvasLayer
    {
        public Action Faded { get; set; }
        
        private Tween _tween;
        private ColorRect _colorRect;

        private readonly ShaderMaterial _square = new ShaderMaterial()
        {
            Shader = ResourceLoader.Load<Shader>("res://assets/Shaders/transition_square.shader")
        };
        private readonly ShaderMaterial _fade = new ShaderMaterial()
        {
            Shader = ResourceLoader.Load<Shader>("res://assets/Shaders/transition_fade.shader")
        };

        private ShaderMaterial Material
        {
            get => (ShaderMaterial) _colorRect.Material;
            set => _colorRect.Material = value;
        }

        public FadeManager()
        {
            _tween = new Tween()
            {
                Name = "Tween",
                Repeat = false,
            };
            _colorRect = new ColorRect()
            {
                Name = "ViewportRect",
                Color = Colors.Transparent,
            };
            PauseMode = PauseModeEnum.Process;
            Layer = 126;
        }

        public override void _Ready()
        {
            _colorRect.RectSize = GetViewport().Size;
            _colorRect.Material = _fade;
            _tween.Connect("tween_all_completed", this, nameof(OnTweenCompleted));
            
            AddChild(_tween);
            AddChild(_colorRect);
        }

        public void DoFade(Color from, Color to, float duration, float delay = 0f)
        {
            from = (from == Colors.Transparent) ? new Color(0f, 0f, 0f, 0f) : from;
            to = (to == Colors.Transparent) ? new Color(0f, 0f, 0f, 0f) : to;
            
            Material = _fade;
            Material.SetShaderParam("from_color", from);
            Material.SetShaderParam("to_color", to);
            
            _tween.InterpolateProperty(Material, "shader_param/progress", 0f, 1f, 
                duration, TransitionType.Linear, EaseType.InOut, delay);
            _tween.Start();
        }

        public void DoSquare(Color color, Vector2 position, float duration, bool backwards, float delay = 0f)
        {
            color = (color == Colors.Transparent) ? new Color(0f, 0f, 0f, 0f) : color;

            Material = _square;
            Material.SetShaderParam("from_color", color);
            Material.SetShaderParam("center",  position.Floor() / GetViewport().Size);
            
            _tween.InterpolateProperty(Material, "shader_param/progress", 
                Convert.ToSingle(backwards), Convert.ToSingle(!backwards), duration, 
                TransitionType.Linear, EaseType.InOut, delay);
            _tween.Start();
        }

        public void Reset()
        {
            _colorRect.Material = null;
            _tween.StopAll();
        }

        private void OnTweenCompleted()
        {
            if (Faded != null)
            {
                Faded.Invoke();
                Faded = null;
            }
        }
    }
}