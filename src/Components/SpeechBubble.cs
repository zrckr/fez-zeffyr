using Godot;

namespace Zeffyr.Components
{
    public class SpeechBubble : PanelContainer
    {
        [Export(PropertyHint.Range, "0.01, 0.1, 0.01")]
        public float CharTime = 0.05f;

        [Node] private Tween Tween { get; set; } = default;

        [Node] private Label Label { get; set; } = default;

        [Node] private Timer Timer { get; set; } = default;

        public static SpeechBubble CreateInstance()
        {
            var scene = ResourceLoader.Load<PackedScene>("res://scenes/SpeechBubble.tscn");
            Node node = scene.Instance();
            if (Engine.GetMainLoop() is SceneTree tree)
                tree.Root.AddChild(node);
            return node.GetChild<SpeechBubble>(0);
        }

        private SpeechBubble() { }

        public override void _Ready()
        {
            this.InjectNodes();
            Visible = false;
            Timer.Connect("timeout", this, nameof(OnEnd));
        }

        public void SetText(string text, Vector3? origin, float waitTime = 3f)
        {
            Visible = true;
            if (!string.IsNullOrEmpty(text))
            {
                Label.Text = text;
                RectGlobalPosition = Vector2.Zero;

                if (origin.HasValue)
                {
                    Camera camera = GetViewport().GetCamera();
                    RectGlobalPosition = camera.UnprojectPosition(origin.Value);
                }

                float height = Label.GetFont("font").GetStringSize(Label.Text).y;
                float lines = Mathf.RoundToInt(Label.Text.Length / 12f) + 1f;
                RectGlobalPosition += Vector2.Up * height * lines;

                float duration = Label.Text.Length * CharTime;
                Tween.RemoveAll();
                Tween.InterpolateProperty(Label, "percent_visible", 0f, 1f, duration);
                Tween.Start();

                Timer.WaitTime = waitTime + duration;
                Timer.Start();
            }
        }

        public void OnEnd()
        {
            Timer.Stop();
            Tween.StopAll();
            Visible = false;
            QueueFree();
        }
    }
}