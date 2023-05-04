using Godot;
using System;
using Zeffyr.Components;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Structure.Physics;

namespace Zeffyr.Managers
{
    public class DebugManager : Node
    {
        [RootNode] protected CanvasLayer Console;

        [RootNode] protected GameStateManager GameState;

        [RootNode] protected TimeManager TimeManager;

        [RootNode] protected DebuggingBag DebuggingBag;

        [RootNode] protected FadeManager FadeManager;

        [RootNode] protected MusicManager MusicManager;

        protected GameCamera GameCamera => GetViewport().GetCamera()?.GetParentOrNull<GameCamera>();

        protected Gomez FezDude => GetTree().Root.FindNode("Gomez", true, false) as Gomez;

        protected Level Level => (Level) GetTree().CurrentScene;

        public bool ConsoleShowed { get; private set; }
        public bool FreeMode { get; private set; }

        public override void _Ready()
        {
            PauseMode = PauseModeEnum.Process;
            
            this.InjectNodes();
            this.RegisterCommands();
            this.SetProcess(OS.IsDebugBuild());

            Console.Connect("console_showed", this, nameof(OnConsoleShowed));
        }

        private void OnConsoleShowed(bool showed)
        {
            ConsoleShowed = showed;
            if (GameCamera != null)
                GameCamera.PauseMode = (showed) ? PauseModeEnum.Inherit : PauseModeEnum.Process;
        }

        public override void _Process(float delta)
        {
            Gomez fezDude = FezDude;
            if (fezDude is not null)
            {
                DebuggingBag.Add("$", "vel:", fezDude.Velocity);
                DebuggingBag.Add("$", "glob:", fezDude.GlobalVelocity);
                DebuggingBag.Add("$", "curr:", fezDude.Action);
                DebuggingBag.Add("$", "last:", fezDude.LastAction);
                DebuggingBag.Add("$", "bg:", fezDude.InBackground);
                DebuggingBag.Add("$", "air:", fezDude.AirTime);

                DebuggingBag.Add("@", "pos:", fezDude.Origin);
                DebuggingBag.Add("@", "rot:", GameCamera.Orthogonal);
                DebuggingBag.Add("@", "face:", fezDude.FacingDirection);
                DebuggingBag.Add("@", "floor:", fezDude.OnFloor, "->", fezDude.Floor?.Transform.origin);
                DebuggingBag.Add("@", "wall:", fezDude.OnWall, "->", fezDude.Wall);
                DebuggingBag.Add("@", "ceil:", fezDude.OnCeiling, "->", fezDude.Ceiling);

                if (fezDude.HeldBody is not null)
                    DebuggingBag.Add("@", "held:", fezDude.HeldBody.Transform.origin);

                if (fezDude.PushedBody is not null)
                    DebuggingBag.Add("@", "push:", fezDude.PushedBody.Transform.origin);

                if (fezDude.CarriedBody is not null)
                    DebuggingBag.Add("@", "carry:", fezDude.CarriedBody.Transform.origin);
            }
        }

        public void ConsolePrint(object message, string endLine = "\n")
        {
            Console.Call("write", message + endLine);
        }

        [ConsoleCommand("godot_pause")]
        public void GodotTreePaused()
        {
            GetTree().Paused = !GetTree().Paused;
        }

        [ConsoleCommand("godot_speed")]
        [ConsoleArgument("speed", Variant.Type.Real)]
        public void GodotTimeScale(float speed)
        {
            Engine.TimeScale = speed;
        }
        
        [ConsoleCommand("gomez_free")]
        [ConsoleArgument("mode", Variant.Type.Bool)]
        public void SwitchFreeMode(bool mode)
        {
            FreeMode = mode;
        }

        [ConsoleCommand("sound_volume")]
        [ConsoleArgument("factor", Variant.Type.Real)]
        public void AudioServerChangeVolume(float factor)
        {
            MusicManager.MasterVolume = factor;
        }

        [ConsoleCommand("debug_toggle")]
        [ConsoleArgument("mode", Variant.Type.Bool)]
        public void SwitchDebugInfo(bool mode)
        {
            DebuggingBag.Visible = mode;
        }

        [ConsoleCommand("time_hour")]
        [ConsoleArgument("hour", Variant.Type.Int)]
        public void TimeManagerHour(int hour)
        {
            TimeManager.SetHourSmooth(hour);
        }

        [ConsoleCommand("time_factor")]
        [ConsoleArgument("factor", Variant.Type.Real)]
        public void TimeManagerFactor(float factor)
        {
            TimeManager.TimeFactor = factor;
        }

        [ConsoleCommand("data_small")]
        [ConsoleArgument("count", Variant.Type.Int)]
        public void SaveDataSmallCubesCount(int count)
        {
            GameState.SaveData.SmallCubes = count;
            if (GameState.SaveData.SmallCubes >= 8)
            {
                GameState.SaveData.BigCubes += GameState.SaveData.SmallCubes / 8;
                GameState.SaveData.SmallCubes %= 8;
            }

            GameState.SaveData.ScoreDirty = true;
            GameState.EmitSignal(nameof(GameStateManager.OnHudChanged));
        }

        [ConsoleCommand("data_big")]
        [ConsoleArgument("count", Variant.Type.Int)]
        public void SaveDataBigCubesCount(int count)
        {
            GameState.SaveData.BigCubes = count;
            GameState.SaveData.ScoreDirty = true;
            GameState.EmitSignal(nameof(GameStateManager.OnHudChanged));
        }

        [ConsoleCommand("data_keys")]
        [ConsoleArgument("count", Variant.Type.Int)]
        public void SaveDataKeysCount(int count)
        {
            GameState.SaveData.Keys = count;
            GameState.EmitSignal(nameof(GameStateManager.OnHudChanged));
        }

        [ConsoleCommand("data_anti")]
        [ConsoleArgument("count", Variant.Type.Int)]
        public void SaveDataAntiCubesCount(int count)
        {
            GameState.SaveData.AntiCubes = count;
            GameState.SaveData.ScoreDirty = true;
            GameState.EmitSignal(nameof(GameStateManager.OnHudChanged));
        }

        [ConsoleCommand("gomez_reset")]
        public void FezDudeReset()
        {
            FezDude.Reset();
        }

        [ConsoleCommand("gomez_action")]
        [ConsoleArgument("action", Variant.Type.String)]
        public void FezDudeAction(string action)
        {
            if (Enum.TryParse(action, out ActionType actionType))
                FezDude.Action = actionType;
            else ConsolePrint($"Invalid argument for {nameof(ActionType)}: {action}!");
        }

        [ConsoleCommand("gomez_carry")]
        [ConsoleArgument("mass", Variant.Type.Real)]
        public void FezDudeCarry(float mass)
        {
            if (mass < 0f)
            {
                FezDude.CarriedBody = null;
            }
            else
            {
                FezDude.PushedBody = null;
                FezDude.CarriedBody = new Pickup()
                {
                    IsHeavy = (mass >= 1f)
                };
            }
        }

        [ConsoleCommand("camera_shake")]
        [ConsoleArgument("amount", Variant.Type.Real)]
        [ConsoleArgument("freq", Variant.Type.Real)]
        [ConsoleArgument("amp", Variant.Type.Real)]
        public void CameraShake(float distance, float duration)
        {
            GameCamera.Shake(distance, duration);
        }

        [ConsoleCommand("camera_flash")]
        [ConsoleArgument("duration", Variant.Type.Real)]
        public void CameraFlash(float duration)
        {
            GameCamera.Flash(Colors.White, duration);
        }

        [ConsoleCommand("camera_view")]
        [ConsoleArgument("viewpoint", Variant.Type.String)]
        public void CameraViewpoint(string viewpoint)
        {
            bool parsed = Enum.TryParse(viewpoint, out Orthogonal view);
            if (view == Orthogonal.None || !parsed)
            {
                ConsolePrint($"Invalid argument for {nameof(Orthogonal)}: {viewpoint}!");
            }
            else
            {
                GameCamera.ChangeRotation(view);
            }
        }

        [ConsoleCommand("godot_find")]
        [ConsoleArgument("nodeName", Variant.Type.String)]
        public void GodotFindNodeInfo(string nodeName)
        {
            var node = GetTree().Root.FindNode(nodeName, true, false);
            if (node is null)
            {
                ConsolePrint($"Node {nodeName} is not found!");
            }
            else
            {
                ConsolePrint("Name: " + node.Name);
                if (node is Spatial spatial)
                {
                    ConsolePrint("Origin: " + spatial.GlobalTransform.origin);
                    ConsolePrint("Degrees: " + spatial.RotationDegrees);
                    ConsolePrint("Scale: " + spatial.Scale);
                    if (node is CollisionObject obj)
                    {
                        Vector3 extents = obj.ShapeOwnerGetExtents(0);
                        PhysicsLayer layers = obj.GetLayer();
                        ConsolePrint("Shape: " + extents);
                        ConsolePrint("Layer: " + layers);
                    }
                }
            }
        }

        [ConsoleCommand("input_rumble")]
        [ConsoleArgument("motor", Variant.Type.String)]
        [ConsoleArgument("amount", Variant.Type.Real)]
        [ConsoleArgument("duration", Variant.Type.Real)]
        [ConsoleArgument("easing", Variant.Type.Real)]
        public void JoypadVibrate(bool strong, float amount, float duration, float easing)
        {
            Joypad.Motors motor = (strong) ? Joypad.Motors.LeftStrong : Joypad.Motors.RightWeak;
            Func<float, float> easingFunc = (x) => Mathf.Ease(x, easing);
            FzInput.Joypad.Vibrate(motor, amount, duration, easingFunc);
        }

        [ConsoleCommand("level_load2")]
        [ConsoleArgument("level", Variant.Type.String)]
        [ConsoleArgument("volume", Variant.Type.Int)]
        public void LevelLoad(string level, int volume)
        {
            try
            {
                GameState.ChangeLevel(level, volume);
            }
            catch (Exception ex)
            {
                ConsolePrint(ex.Message);
            }
        }

        [ConsoleCommand("level_load1")]
        [ConsoleArgument("level", Variant.Type.String)]
        public void LevelLoad(string level)
        {
            LevelLoad(level, 0);
        }

        [ConsoleCommand("level_reset")]
        public void LevelReset()
        {
            GetTree().ReloadCurrentScene();
        }

        [ConsoleCommand("fade_square")]
        [ConsoleArgument("duration", Variant.Type.Real)]
        [ConsoleArgument("backwards", Variant.Type.Bool)]
        public void FadeSquare(float duration, bool backwards)
        {
            Vector2 position = GameCamera.UnprojectPosition(FezDude.Origin);
            FadeManager.DoSquare(Colors.Black, position, duration, backwards);
        }

        [ConsoleCommand("godot_free")]
        [ConsoleArgument("nodeName", Variant.Type.String)]
        public void GodotFreeNode(string nodeName)
        {
            var node = GetTree().Root.FindNode(nodeName, true, false);
            if (node is null)
            {
                ConsolePrint($"Node {nodeName} is not found!");
            }
            else
            {
                ConsolePrint($"Node {nodeName} is exiting the tree.");
                node.QueueFree();
            }
        }

        [ConsoleCommand("state_save")]
        [ConsoleArgument("slot", Variant.Type.Int)]
        public void GameStateSave(int slot)
        {
            int active = GameState.SaveSlot;
            GameState.SaveSlot = slot;
            GameState.SaveGame();
            GameState.SaveSlot = active;
        }

        [ConsoleCommand("state_load")]
        [ConsoleArgument("slot", Variant.Type.Int)]
        public void GameStateLoadJson(int slot)
        {
            int active = GameState.SaveSlot;
            GameState.SaveSlot = slot;
            GameState.LoadGame();
            GameState.SaveSlot = active;
        }

        [ConsoleCommand("speech_text")]
        [ConsoleArgument("text", Variant.Type.String)]
        public void SpeechBubbleSetText(string text)
        {
            SpeechBubble.CreateInstance().SetText(text, FezDude.Origin + Vector3.Up);
        }

        [ConsoleCommand("state_inactive")]
        public void GameStateShowInactiveNodes()
        {
            string list = string.Join("\n", GameState.SaveData.ThisLevel.InactiveNodes);
            ConsolePrint(list);
        }

        [ConsoleCommand("water_height")]
        [ConsoleArgument("height", Variant.Type.Real)]
        public void WaterSetHeight(float height)
        {
            if (Level.Water != null)
            {
                Level.Water.SetHeight(height);
            }
            else
            {
                ConsolePrint($"There's no Water node on {Level.Name}");
            }
        }

        [ConsoleCommand("water_raise")]
        [ConsoleArgument("ups", Variant.Type.Real)]
        [ConsoleArgument("height", Variant.Type.Real)]
        public void WaterRaise(float ups, float height)
        {
            if (Level.Water != null)
            {
                Level.Water.RaiseWater(ups, height);
            }
            else
            {
                ConsolePrint($"There's no Water node on {Level.Name}");
            }
        }

        [ConsoleCommand("water_stop")]
        public void WaterStop()
        {
            if (Level.Water != null)
            {
                Level.Water.StopWater();
            }
            else
            {
                ConsolePrint($"There's no Water node on {Level.Name}");
            }
        }
    }
}