using System;

namespace Zeffyr.Structure
{
    public class Settings : ICloneable
    {
        public bool Fullscreen { get; set; }

        public int Width { get; set; }
        
        public int Height { get; set; }

        public bool VSync { get; set; }

        public float SoundVolume { get; set; }

        public float MusicVolume { get; set; }
        
        public bool Vibration { get; set; }
        
        public bool DisableController { get; set; }
        
        public int DeadZone { get; set; }

        public bool PauseOnLostFocus { get; set; }

        public bool InvertLookX { get; set; }

        public bool InvertLookY { get; set; }
        
        public string Language { get; set; }

        public Settings()
        {
            Fullscreen = false;
            Width = 1280;
            Height = 720;
            VSync = false;
            SoundVolume = 1f;
            MusicVolume = 1f;
            Vibration = true;
            DisableController = false;
            DeadZone = 40;
            PauseOnLostFocus = true;
            InvertLookX = false;
            InvertLookY = false;
            Language = "en";
        }

        public object Clone() => this.MemberwiseClone();
    }
}