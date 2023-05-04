using System;
using Godot;
using Zeffyr.Managers;
using Zeffyr.Structure.Input;
using Zeffyr.Tools;

namespace Zeffyr.Structure
{
    public static class SettingsHelper
    {
        private const string SettingsPath = "user://Settings.json";
        public static Settings Settings = new Settings();
        public static bool FirstOpen;
        
        public static Tuple<int, int>[] ScreenResolutions = new Tuple<int, int>[]
        {
            new Tuple<int, int>(1920, 1080),
            new Tuple<int, int>(1366, 768),
            new Tuple<int, int>(1280, 1024),
            new Tuple<int, int>(1280, 720),
            new Tuple<int, int>(1024, 768),
            new Tuple<int, int>(800, 600),
        };

        public static void Load()
        {
            FirstOpen = !FileOperations.Exists(SettingsPath);
            if (!FirstOpen)
                FileOperations.TryLoadJson(SettingsPath, out Settings);
        }

        public static void Save()
        {
            FileOperations.TryWriteJson(Settings, SettingsPath);
        }

        public static void Apply(Node calledFrom)
        {
            OS.WindowSize = new Vector2(Settings.Width, Settings.Height);
            OS.WindowFullscreen = Settings.Fullscreen;
            OS.VsyncEnabled = Settings.VSync;

            //OS.MinWindowSize = OS.WindowSize;
            OS.MaxWindowSize = OS.GetScreenSize();
            
            FzInput.Joypad.Enabled = !Settings.DisableController;
            FzInput.Joypad.Vibration = Settings.Vibration;
            FzInput.Joypad.Deadzone = Settings.DeadZone / 100f;
            FzInput.InvertAxes = new Vector2()
            {
                x = Settings.InvertLookX ? -1f : 1f,
                y = Settings.InvertLookY ? -1f : 1f
            };
            
            TranslationServer.SetLocale(Settings.Language);
            
            MusicManager music = calledFrom.GetNodeOrNull<MusicManager>("/root/MusicManager");
            GameStateManager gameState = calledFrom.GetNodeOrNull<GameStateManager>("/root/GameState");
            
            music.MusicVolume = Settings.MusicVolume;
            music.SoundVolume = Settings.SoundVolume;
            gameState.PauseOnLostFocus = Settings.PauseOnLostFocus;
        }
    }
}