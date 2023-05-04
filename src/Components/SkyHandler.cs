using Godot;
using System.Linq;
using System.Collections.Generic;
using Zeffyr.Managers;
using Zeffyr.Structure;
using Zeffyr.Tools;
using Random = Zeffyr.Tools.Random;

namespace Zeffyr.Components
{
    public class SkyHandler : CanvasLayer
    {
        [Export] public readonly StreamTexture Background;

        [Export] public readonly float WindSpeed = 1f;

        [Export] public readonly float Density = 1f;

        [Export] public readonly float FogDensity = 0.02f;

        [Node("SkyRect", true)] public Sprite Sky;
        
        [Node("Shadows/Texture", true)] public Sprite Shadows;
        
        [Node("Stars", true)] public Sprite Stars;

        [Node("Clouds", true)] public CanvasLayer Clouds;
       
        [Node(("Layers"), true)] public Node Layers;

        [RootNode] public TimeManager TimeManager;

        [CameraHelper(typeof(GameCamera))] public GameCamera GameCamera;

        public Gradient SkyGradient;
        
        public List<Sprite> CloudSprites;

        public List<ParallaxLayer> LayerSprites;

        public float[] CloudSpeeds;

        public override void _Ready()
        {
            this.InjectNodes();
            this.Initialize();
        }

        public override void _Process(float delta)
        {
            UpdateTimeOfDay();
            ShowStars(delta);
            
            if (CloudSprites.Count != 0)
                MoveClouds(delta);
        }

        private void Initialize()
        {
            GradientTexture gradient = (GradientTexture) Sky.Texture;
            Vector2 screenSize = GetViewport().Size;

            gradient.Width = (int) screenSize.y;
            Sky.Scale = new Vector2(1f, -screenSize.x);
            
            SkyGradient = gradient.Gradient;
            SkyGradient.Offsets = Mathz.UnitRange(1f / 8f).ToArray();

            if (Shadows != null)
            {
                Vector2 size = Shadows.Texture.GetSize();
                Shadows.Position = new Vector2()
                {
                    x = -size.x,
                    y = Random.Range(-size.y / 2f, size.y / 2f)
                };
            }

            CloudSprites = Clouds.GetChildrenList<Sprite>();
            LayerSprites = Layers.GetChildrenList<ParallaxLayer>();
            CloudSpeeds = Random.Sequence(CloudSprites.Count, 8f, 24f).ToArray();
            
            GameCamera.Connect(nameof(BaseCamera.Rotating), this, nameof(MoveClouds));
        }
        
        private void MoveClouds(float delta)
        {
            for (int i = 0; i < CloudSprites.Count; i++)
            {
                Sprite sprite = CloudSprites[i];
                Vector2 position = sprite.Position;
                position.x = Mathf.MoveToward(position.x, float.MaxValue, CloudSpeeds[i] * WindSpeed * delta);
                sprite.Position = position;

                if (delta > 0.05f && delta < 1f)
                {
                    float sign = GameCamera.Orthogonal.DistanceTo(GameCamera.LastOrthogonal);
                    sprite.Position += sign * (1f - delta) * GameCamera.Size * Vector2.Right;
                }
                
                WrapPosition(sprite);
                SetModulate(sprite, TimeManager.DayTransition, 0.5f);
            }

            if (Shadows != null)
            {
                Vector2 position = Shadows.Position;
                position.x = Mathf.MoveToward(position.x, -float.MaxValue, 0.5f * WindSpeed * delta);
                Shadows.Position = position;
                
                WrapPosition(Shadows);
                SetModulate(Shadows, TimeManager.DayTransition, 0.33f);
            }
        }
        
        private void ShowStars(float delta)
        {
            if (TimeManager.IsDayPhase(DayPhase.Day))
            {
                Vector2 position = Stars.Position;
                position.x = Mathf.MoveToward(position.x, float.MaxValue, TimeManager.DayFraction * delta);
                Stars.Position = position;
                
                WrapPosition(Stars);
            }
            SetModulate(Stars, TimeManager.NightTransition);
        }

        private void WrapPosition(Sprite sprite)
        {
            Vector2 screenSize = GetViewport().Size;
            Vector2 size = Stars.Texture.GetSize();
            Vector2 position = sprite.Position;
            
            position.x = Mathf.Wrap(position.x, -size.x, screenSize.x);
            position.y = Mathf.Wrap(position.y, -size.y, screenSize.y);
            sprite.Position = position;
        }

        private void SetModulate(Sprite sprite, float weight, float defaultValue = 1f)
        {
            Color modulate = sprite.Modulate;
            modulate.a = defaultValue * weight;
            sprite.Modulate = modulate;
        }

        private void UpdateTimeOfDay()
        {
            Image image = Background.GetData();
            image.Lock();
            
            Vector2 size = image.GetSize();
            Color[] colors = new Color[(int) size.y];
            int next = (TimeManager.Hour + 1) % (int) size.x;

            for (int i = 0; i < (int) size.y; i++)
            {
                Color first = image.GetPixel(TimeManager.Hour, i);
                Color second = image.GetPixel(next, i);
                colors[i] = first.LinearInterpolate(second, TimeManager.NextHourContribution);
            }
            
            SkyGradient.Colors = colors;
        }
    }
}