using Godot;
using System.Collections.Generic;
using Zeffyr.Tools;

namespace Zeffyr.Structure.Input
{
    public static class FzInput
    {
        public static Mouse Mouse { get; private set; }

        public static Joypad Joypad { get; private set; }

        public static InputEvent Event { get; set; }

        public static Vector2 InvertAxes { get; set; } = Vector2.One;
        
        public static Vector2 Movement
        {
            get => new Vector2()
            {
                x = Godot.Input.GetActionStrength(InputAction.Right) - Godot.Input.GetActionStrength(InputAction.Left),
                y = Godot.Input.GetActionStrength(InputAction.Up) - Godot.Input.GetActionStrength(InputAction.Down),
            };
        }

        public static Vector2 FreeLook
        {
            get
            {
                Vector2 freeLook = Vector2.Zero;
                if (Event is InputEventJoypadMotion)
                {
                    freeLook = Joypad.RightStick;
                }
                else
                {
                    switch (Mouse.LeftButton)
                    {
                        case Mouse.ButtonState.Clicked:
                            _mouseClickPos = Mouse.Position;
                            break;
                        case Mouse.ButtonState.Dragging:
                            if (_mouseClickPos.HasValue)
                            {
                                Vector2 drag = (Mouse.Position - _mouseClickPos.Value).Normalized();
                                float up = drag.Dot(Vector2.Down);
                                float down = drag.Dot(Vector2.Right);

                                bool distanceTo = _mouseClickPos.Value.DistanceTo(Mouse.Position) >= MouseDragThreshold;
                                bool look = up >= MouseDragDeadzone || up <= -MouseDragDeadzone ||
                                            down >= MouseDragDeadzone || down <= -MouseDragDeadzone;

                                if (distanceTo && look)
                                    freeLook = drag;
                            }

                            break;
                        case Mouse.ButtonState.Released:
                            if (!freeLook.IsZeroApprox())
                            {
                                freeLook.x = Mathf.Lerp(freeLook.x, 0f, 0.1f);
                                freeLook.y = Mathf.Lerp(freeLook.y, 0f, 0.1f);
                            }

                            break;
                        default:
                            _mouseClickPos = null;
                            break;
                    }
                }
                freeLook *= InvertAxes;
                return freeLook;
            }
        }
        
        public static bool DevicesInitialized { get; private set; }
        
        public static bool IsJustPressed(string action)
        {
            return Godot.Input.IsActionPressed(action);
        }

        public static bool IsPressed(string action)
        {
            return Godot.Input.IsActionPressed(action);
        }

        public static bool IsJustReleased(string action)
        {
            return Godot.Input.IsActionJustReleased(action);
        }

        public static bool AnyPressed(string action)
        {
            return IsPressed(action) || IsJustPressed(action);
        }

        public static bool IsReleased(string action)
        {
            return (!IsPressed(action) && !IsJustReleased(action) && !IsJustReleased(action));
        }

        public static void LoadDevices(Node caller)
        {
            if (caller != null)
            {
                DevicesInitialized = true;
                Joypad = new Joypad();
                Mouse = new Mouse();
                caller.AddChild(Joypad);
                caller.AddChild(Mouse);
            }
        }
        
        public static void LoadCustomCursors()
        {
            foreach (var pair in Cursors)
            {
                StreamTexture texture = ResourceLoader.Load<StreamTexture>(pair.Value);
                Image data = texture.GetData();
                (int x, int y) = (data.GetWidth() * CursorSizeMultiplier, data.GetHeight() * CursorSizeMultiplier);
                data.Resize(x, y, Image.Interpolation.Nearest);

                ImageTexture imageTexture = new ImageTexture();
                imageTexture.Flags = 0;
                imageTexture.CreateFromImage(data);

                Godot.Input.SetCustomMouseCursor(imageTexture, pair.Key, (Vector2.One * y) / (2f * CursorSizeMultiplier));
            }
        }

        private static readonly Dictionary<Godot.Input.CursorShape, string> Cursors = new Dictionary<Godot.Input.CursorShape, string>()
        {
            [Godot.Input.CursorShape.PointingHand] = "res://assets/Other Textures/cursor/cursor_clicker_a.png",
            [Godot.Input.CursorShape.Wait] = "res://assets/Other Textures/cursor/cursor_clicker_b.png",
            [Godot.Input.CursorShape.Drag] = "res://assets/Other Textures/cursor/cursor_grabber.png",
            [Godot.Input.CursorShape.Arrow] = "res://assets/Other Textures/cursor/cursor_pointer.png",
        };

        private const int CursorSizeMultiplier = 2;
        private const float MouseDragThreshold = 10f;
        private const float MouseDragDeadzone = 0.5f;
        private static Vector2? _mouseClickPos;
    }
}