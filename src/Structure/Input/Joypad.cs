using Godot;
using System;
using System.Diagnostics;
using System.Linq;
using Zeffyr.Tools;

namespace Zeffyr.Structure.Input
{
    [DebuggerDisplay("{DeviceId}: {DeviceName}, {Guid}")]
    public class Joypad : Node
    {
        public enum Motors
        {
            None,
            LeftStrong,
            RightWeak
        }

        public int DeviceId { get; set; }

        public string DeviceName => Godot.Input.GetJoyName(DeviceId);

        public string Guid => Godot.Input.GetJoyGuid(DeviceId);

        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                SetProcess(value);
            }
        }
        
        public bool Vibration { get; set; } = true;

        public float Deadzone { get; set; } = 0.4f;

        public Vector2 LeftStick { get; private set; }

        public Vector2 RightStick { get; private set; }

        private bool _enabled = true;
        private VibrationState _leftMotor;
        private VibrationState _rightMotor;
        
        public override void _Ready()
        {
            int id = Godot.Input.GetConnectedJoypads()
                .OfType<int>()
                .DefaultIfEmpty(-1).First();
            
            DeviceId = id;
            Godot.Input.Singleton.Connect("joy_connection_changed", this, 
                nameof(OnJoyConnectionChanged));
        }

        public void Vibrate(Motors motor, float amount, float duration, Func<float, float> easing = null)
        {
            easing ??= Easing.Linear;
            VibrationState state = new VibrationState(amount, duration, easing);

            if (motor == Motors.RightWeak)
                _rightMotor = state;
            if (motor == Motors.LeftStrong)
                _leftMotor = state;
        }

        private void OnJoyConnectionChanged(int device, bool connected)
        {
            DeviceId = (connected) ? device : -1;
        }

        public override void _Process(float delta)
        {
            switch (OS.GetName())
            {
                case "Windows":
                case { } s when s == "X11" && (DeviceName.Contains("PS4") || DeviceName.Contains("DualShock")):

                    LeftStick = new Vector2()
                    {
                        x = Godot.Input.GetJoyAxis(DeviceId, (int) JoystickList.Axis0),
                        y = Godot.Input.GetJoyAxis(DeviceId, (int) JoystickList.Axis1),
                    };
                    RightStick = new Vector2()
                    {
                        x = Godot.Input.GetJoyAxis(DeviceId, (int) JoystickList.Axis2),
                        y = Godot.Input.GetJoyAxis(DeviceId, (int) JoystickList.Axis3),
                    };
                    break;

                case "X11":
                case "OSX":
                    LeftStick = new Vector2()
                    {
                        x = Godot.Input.GetJoyAxis(DeviceId, (int) JoystickList.Axis1),
                        y = Godot.Input.GetJoyAxis(DeviceId, (int) JoystickList.Axis2),
                    };
                    RightStick = new Vector2()
                    {
                        x = Godot.Input.GetJoyAxis(DeviceId, (int) JoystickList.Axis3),
                        y = Godot.Input.GetJoyAxis(DeviceId, (int) JoystickList.Axis4),
                    };
                    break;
            }

            if (LeftStick.Length() < Deadzone)
                LeftStick = Vector2.Zero;
            else
                LeftStick = LeftStick.Normalized() * ((LeftStick.Length() - Deadzone) / (1 - Deadzone));

            if (RightStick.Length() < Deadzone)
                RightStick = Vector2.Zero;
            else
                RightStick = RightStick.Normalized() * ((RightStick.Length() - Deadzone) / (1 - Deadzone));

            if (Vibration)
            {
                if (_leftMotor.Active)
                    _leftMotor.Update(delta);
                if (_rightMotor.Active)
                    _rightMotor.Update(delta);

                if (Mathf.IsEqualApprox(_leftMotor.LastAmount, _leftMotor.CurrentAmount) &&
                    Mathf.IsEqualApprox(_rightMotor.LastAmount, _rightMotor.CurrentAmount))
                {
                    Godot.Input.StopJoyVibration(DeviceId);
                }
                else
                {
                    // Fallback in the next frame, if joypad is disconnected during rumble
                    Godot.Input.StartJoyVibration(DeviceId,
                        _rightMotor.CurrentAmount, _leftMotor.CurrentAmount, delta);
                }
            }
        }
    }
}