using Godot;

namespace Zeffyr.Structure.Input
{
    public class Mouse : Node
    {
        private const float ScrollAmount = 0.08f;
        
        public enum ButtonState
        {
            Released,
            Clicked,
            DoubleClicked,
            Dragging,
        }

        public Godot.Input.MouseMode Mode
        {
            get => Godot.Input.GetMouseMode();
            set => Godot.Input.SetMouseMode(value);
        }

        public ButtonState LeftButton { get; private set; }
        
        public ButtonState RightButton { get; private set; }
        
        public ButtonState MiddleButton { get; private set; }

        public float Pressure { get; private set; }
        
        public Vector2 Position { get; private set; }
        
        public Vector2 Relative { get; private set; }
        
        public Vector2 Tilt { get; private set; }
        
        public float Scroll { get; private set; }

        public override void _Input(InputEvent @event)
        {
            switch (@event)
            {
                case InputEventMouseButton mb:
                    ButtonList button = (ButtonList) mb.ButtonIndex;

                    ButtonState state = ButtonState.Released;
                    if (mb.Pressed)
                        state = (mb.Doubleclick) ? ButtonState.DoubleClicked : ButtonState.Clicked;
                    
                    switch (button)
                    {
                        case ButtonList.Left:
                            LeftButton = state;
                            break;
                        case ButtonList.Right:
                            RightButton = state;
                            break;
                        case ButtonList.Middle:
                            MiddleButton = state;
                            break;
                        case ButtonList.WheelUp:
                            Scroll += ScrollAmount;
                            break;
                        case ButtonList.WheelDown:
                            Scroll -= ScrollAmount;
                            break;
                    }
                    break;
                
                case InputEventMouseMotion mm:
                    Pressure = mm.Pressure;
                    Relative = mm.Relative;
                    Tilt = mm.Tilt;
                    Position = mm.Position;
                    
                    if (LeftButton == ButtonState.Clicked || LeftButton == ButtonState.DoubleClicked)
                        LeftButton = ButtonState.Dragging;
                    
                    if (RightButton == ButtonState.Clicked || RightButton == ButtonState.DoubleClicked)
                        RightButton = ButtonState.Dragging;
                    
                    if (MiddleButton == ButtonState.Clicked || MiddleButton == ButtonState.DoubleClicked)
                        MiddleButton = ButtonState.Dragging;
                    break;
            }
        }
    }
}