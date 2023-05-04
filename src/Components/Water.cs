using Godot;
using System;
using System.Collections.Generic;
using Zeffyr.Managers;
using Zeffyr.Structure;

namespace Zeffyr.Components
{
    public class Water : CanvasLayer
    {
        [Export] public float Height { get; private set; }

        [Export] public LiquidType Type { get; private set; }

        [Node] public Tween Tween;

        [Node] public ColorRect LiquidBody;

        [RootNode] public GameStateManager GameState;

        private readonly Stack<object> _stopStack = new Stack<object>();
        private float _waterSpeed;
        private float _originalHeight;
        private Func<float, bool> _onAct;
        private Action _onEnd;

        private ShaderMaterial Shader => LiquidBody.Material as ShaderMaterial;

        private readonly Dictionary<LiquidType, Color> _colorSchemes = new Dictionary<LiquidType, Color>()
        {
            {
                LiquidType.Water,
                Color.Color8(61, 117, 254)
            },
            {
                LiquidType.Blood,
                Color.Color8(174, 26, 0)
            },
            {
                LiquidType.Sewer,
                Color.Color8(82, (int) sbyte.MaxValue, 57)
            },
            {
                LiquidType.Lava,
                Color.Color8(209, 0, 0)
            },
            {
                LiquidType.Purple,
                Color.Color8(194, 1, 171)
            },
            {
                LiquidType.Green,
                Color.Color8(47, 255, 139)
            }
        };

        public override void _Ready() => this.InjectNodes();

        public override void _Process(float delta)
        {
            if (_onAct != null)
            {
                bool result = _onAct.Invoke(delta);
                if (result)
                {
                    _onAct = null;
                    _onEnd.Invoke();
                    _onEnd = null;
                }
            }
            
            float speed = !Mathf.IsZeroApprox(_waterSpeed) ? Mathf.Abs(_waterSpeed) : 1f;
            Shader.SetShaderParam("speed", speed);
            Shader.SetShaderParam("blue_tint", _colorSchemes[Type]);

            Camera camera = GetViewport().GetCamera();
            LiquidBody.RectGlobalPosition = camera.UnprojectPosition(Vector3.Up * (Height - 0.4f));
            LiquidBody.RectGlobalPosition *= Vector2.Down;
        }

        public void SetHeight(float height)
        {
            int sign = Mathf.Sign(height - Height);
            _waterSpeed = 1.2f;
            
            _onAct += (delta) =>
            {
                if (Mathf.Sign(height - Height) == sign)
                {
                    Height += _waterSpeed * delta * sign;
                    return false;
                }

                return true;
            };
            _onEnd += () =>
            {
                _waterSpeed = 0f;
                if (Type == LiquidType.Water)
                    GameState.SaveData.GlobalWaterHeight = _originalHeight - Height;
                else
                    GameState.SaveData.ThisLevel.LastStableWaterHeight = Height;
            };
        }

        public void RaiseWater(float unitsPerSecond, float toHeight)
        {
            int sign = Mathf.Sign(toHeight - Height);
            if (!Mathf.IsZeroApprox(_waterSpeed))
                _stopStack.Push(new object());

            _onAct += (delta) =>
            {
                if (Mathf.Sign(toHeight - Height) == sign && _stopStack.Count <= 0)
                {
                    _waterSpeed = unitsPerSecond * sign;
                    Height += _waterSpeed * delta;
                    return false;
                }

                return true;
            };
            _onEnd += () =>
            {
                _waterSpeed = 0f;
                if (Type == LiquidType.Water)
                    GameState.SaveData.GlobalWaterHeight = _originalHeight - Height;
                else
                    GameState.SaveData.ThisLevel.LastStableWaterHeight = Height;

                if (_stopStack.Count > 0)
                    _stopStack.Pop();
            };
        }

        public void StopWater() => _stopStack.Push(new object());

        public void ReestablishHeight(Gomez gomez)
        {
            _originalHeight = Height;
            if (Type == LiquidType.Water && GameState.SaveData.GlobalWaterHeight.HasValue)
            {
                Height += GameState.SaveData.GlobalWaterHeight.Value;
            }
            else if (!GameState.SaveData.ThisLevel.LastStableWaterHeight.HasValue)
            {
                GameState.SaveData.ThisLevel.LastStableWaterHeight = Height;
            }
            else
            {
                Height = GameState.SaveData.ThisLevel.LastStableWaterHeight.Value;
            }
            
            if (gomez != null && gomez.Origin.y <= Height)
            {
                float diff = gomez.Origin.y - Height - 1f;
                Height += diff;
                if (Type == LiquidType.Water && GameState.SaveData.GlobalWaterHeight.HasValue)
                {
                    SaveData saveData = GameState.SaveData;
                    float? waterLevelModifier = saveData.GlobalWaterHeight;
                    saveData.GlobalWaterHeight = waterLevelModifier.HasValue
                        ? waterLevelModifier.GetValueOrDefault() + diff
                        : new float?();
                }
            }
        }
    }
}