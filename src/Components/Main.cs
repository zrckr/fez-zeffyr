using System;
using Godot;
using Zeffyr.Components.GUI;
using Zeffyr.Components.Map;
using Zeffyr.Managers;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;

namespace Zeffyr.Components
{
    public class Main : Node
    {
        [Export(PropertyHint.File, "*.tscn")]
        public readonly string NewGameLevel;

        [Export(PropertyHint.File, "*.tscn")]
        public readonly string WorldMapScene;
        
        [RootNode] public GameStateManager GameState;
        
        [RootNode] public DebugManager DebugManager;

        [Node("UI/InGameHud")] public InGameHud InGameHud;
        
        [Node("UI/GameMenu")] public GameMenu GameMenu;

        [Node("UI/MapOverlay")] public Control MapOverlay;

        private SceneTree _tree;
        private Level _currentLevel;
        private WorldMap _worldMap;
        
        public override void _Ready()
        {
            this.InjectNodes();
            if (string.IsNullOrEmpty(NewGameLevel))
                throw new ArgumentNullException($"'{nameof(NewGameLevel)}' variable is not set!");

            _worldMap = ResourceLoader.Load<PackedScene>(WorldMapScene).Instance<WorldMap>();
            
            FzInput.LoadDevices(this);
            FzInput.LoadCustomCursors();

            SettingsHelper.Load();
            SettingsHelper.Apply(this);

            _tree = GetTree();
            GameMenu.OpenMenu();
            GameState.FallbackLevelPath = NewGameLevel;
            GameState.InGame = false;
        }

        public override void _Process(float delta)
        {
            if (!GameState.InGame)
                _tree.Paused = GameMenu.Visible;

            if (GameState.InGame && !_tree.Paused && !GameState.Map)
                GameState.SaveData.PlayTime += TimeSpan.FromSeconds(delta).Ticks;
        }

        public override void _UnhandledInput(InputEvent @event)
        {
            FzInput.Event = @event;
            if (@event.IsActionPressed(UiAction.End) && GameState.InGame && !DebugManager.ConsoleShowed)
            {
                _tree.Paused = !_tree.Paused;
                if (_tree.Paused)
                {
                    GameMenu.OpenMenu();
                }
                else
                {
                    GameMenu.ReturnToGame();
                }
                _tree.SetInputAsHandled();
            }

            if (@event.IsActionPressed(UiAction.Home) && !GameMenu.Visible && GameState.InGame && !DebugManager.ConsoleShowed)
            {
                GameState.Map = !GameState.Map;
                if (GameState.Map)
                {
                    _currentLevel = (Level) _tree.CurrentScene;
                    _tree.Root.RemoveChild(_currentLevel);
                    _tree.Root.AddChild(_worldMap);
                    _tree.CurrentScene = _worldMap;
                }
                else
                {
                    _tree.Root.RemoveChild(_worldMap);
                    _tree.Root.AddChild(_currentLevel);
                    _tree.CurrentScene = _currentLevel;
                    _currentLevel = null;
                }

                MapOverlay.Visible = GameState.Map;
                _tree.CurrentScene.RequestReady();
                _tree.SetInputAsHandled();
            }
        }

        public override void _Notification(int what)
        {
            switch (what)
            {
                case MainLoop.NotificationWmQuitRequest:
                    _tree.Root.QueueFree();
                    _tree.Quit();
                    break;
                case MainLoop.NotificationWmFocusOut:
                    if (GameState.PauseOnLostFocus &&
                        !GameMenu.Visible && GameState.InGame && !OS.IsDebugBuild())
                    {
                        _tree.Paused = true;
                        GameMenu.OpenMenu();
                    }
                    break;
            }
        }
    }
}