using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Zeffyr.Managers;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Tools;

namespace Zeffyr.Components.GUI
{
    // NOTE: this class is super shitty...
    public class GameMenu : PanelContainer
    {
        private enum MenuId
        {
            None = -1,
            Root = 0,
            StartNewGame,
            QuitGame,
            SelectSlot,
            Options,
            HowToPlay,
            GameSetup,
            VideoSetup,
            AudioSetup,
            Language,
        }

        private enum SlotMode
        {
            None,
            New,
            Load,
            Erase,
        }

        [Node("Animation")] public readonly AnimationPlayer Animation = default;

        #region Sounds

        [Node("Sounds/Cancel")] public readonly AudioStreamPlayer Cancel = default;

        [Node("Sounds/Confirm")] public readonly AudioStreamPlayer Confirm = default;

        [Node("Sounds/CursorUp")] public readonly AudioStreamPlayer CursorUp = default;

        [Node("Sounds/CursorDown")] public readonly AudioStreamPlayer CursorDown = default;

        [Node("Sounds/ExitGame")] public readonly AudioStreamPlayer ExitGame = default;

        [Node("Sounds/ReturnLevel")] public readonly AudioStreamPlayer ReturnLevel = default;

        [Node("Sounds/ScreenNarrow")] public readonly AudioStreamPlayer ScreenNarrow = default;

        [Node("Sounds/ScreenWiden")] public readonly AudioStreamPlayer ScreenWiden = default;

        [Node("Sounds/SliderInc")] public readonly AudioStreamPlayer SliderInc = default;

        [Node("Sounds/SliderDec")] public readonly AudioStreamPlayer SliderDec = default;

        [Node("Sounds/StartGame")] public readonly AudioStreamPlayer StartGame = default;

        [Node("Sounds/Appear")] public readonly AudioStreamPlayer Appear = default;

        [Node("Sounds/Disappear")] public readonly AudioStreamPlayer Disappear = default;

        #endregion

        [Node("VBox/Hierarchy")] public readonly Node Hierarchy = default;

        [Node("VBox/Header")] public readonly Label Header = default;

        [Node("VBox/Menus")] public readonly PanelContainer Menus = default;

        [Node("VBox/Navigation")] public readonly HBoxContainer Navigation = default;

        [Node("VBox/Navigation/Change")] public readonly HBoxContainer ChangeHint = default;

        [Node("VBox/Navigation/Confirm")] public readonly HBoxContainer ConfirmHint = default;

        [Node("VBox/Navigation/Back")] public readonly HBoxContainer BackHint = default;

        [RootNode] public DebugManager DebugManager;

        [RootNode] public GameStateManager GameState;

        [RootNode] public FadeManager FadeManager;

        private Settings TempSettings { get; set; }

        private MenuId CurrentMenu { get; set; }

        private MenuId PreviousMenu
        {
            get
            {
                Node parent = Hierarchy.FindNode(CurrentMenu.ToString(), true, false).GetParent();
                if (parent.Name != "Hierarchy")
                    return (MenuId) Enum.Parse(typeof(MenuId), parent.Name);
                return MenuId.None;
            }
        }

        private VBoxContainer CurrentMenuInstance => Menus.GetChildOrNull<VBoxContainer>((int) CurrentMenu);

        private List<Button> CurrentMenuButtons => CurrentMenuInstance.GetChildrenList<Button>();

        private readonly Dictionary<string, string> _formattedStrings = new Dictionary<string, string>();

        private bool _selectedIsSlider;
        private bool _inGame;
        private SlotMode _actionOnSlot;
        private int _focusedButtonId;

        public override void _Ready()
        {
            this.Visible = false;
            this.SetProcessInput(false);
            this.InjectNodes();
            this.AttachManyListeners();
        }

        public void OpenMenu()
        {
            _inGame = GetTree().CurrentScene is Level;
            Confirm.Play();
            ChangeMenu(MenuId.Root);
        }

        private async Task PopIn()
        {
            Visible = true;
            SetProcessInput(true);
            Animation.PlayBackwards("fade");
            await ToSignal(Animation, "animation_finished");
        }

        private async Task PopOut()
        {
            Animation.Play("fade");
            await ToSignal(Animation, "animation_finished");
            Visible = false;
            SetProcessInput(false);
        }

        public async void ReturnToGame()
        {
            GetTree().Paused = false;
            ReturnLevel.Play();
            await PopOut();
            await ToSignal(ReturnLevel, "finished");
        }

        private async void QuitGame()
        {
            ExitGame.Play();
            FadeManager.DoFade(Colors.Transparent, Colors.Black, ExitGame.Stream.GetLength());
            FadeManager.Faded += () => GetTree().Quit();
            await PopOut();
        }

        private async void SlotSelected(int slot)
        {
            GameState.SaveSlot = slot;
            if (_actionOnSlot != SlotMode.Erase)
            {
                if (_actionOnSlot == SlotMode.Load && !FileOperations.Exists($"user://SaveSlot{slot}.json"))
                {
                    Disappear.Play();
                    return;
                }

                StartGame.Play();
                FadeManager.DoSquare(Colors.Black, GetViewport().Size / 2f, 1.25f, false);
                FadeManager.Faded += () =>
                {
                    if (_actionOnSlot == SlotMode.New)
                    {
                        GameState.ClearSave();
                        GameState.SaveGame();
                        GameState.WriteSaveData();
                    }
                    else
                    {
                        GameState.LoadGame();
                    }
                    GameState.StartLoading();
                };
                await PopOut();
            }
            else if (_actionOnSlot != SlotMode.None)
            {
                ExitGame.Play();
                GameState.ClearSave();
                ChangeMenu(PreviousMenu);
            }
        }

        private void ResetToDefaults()
        {
            TempSettings = new Settings();
            switch (CurrentMenu)
            {
                case MenuId.GameSetup:
                    ChangeSettings(nameof(TempSettings.PauseOnLostFocus));
                    break;

                case MenuId.VideoSetup:
                    ChangeSettings(nameof(TempSettings.Width));
                    ChangeSettings(nameof(TempSettings.Height));
                    ChangeSettings(nameof(TempSettings.Fullscreen));
                    ChangeSettings(nameof(TempSettings.VSync));
                    break;

                case MenuId.AudioSetup:
                    ChangeSettings(nameof(TempSettings.SoundVolume));
                    ChangeSettings(nameof(TempSettings.MusicVolume));
                    break;
            }

            ApplySettings();
        }

        private void ReadSettings()
        {
            TempSettings = SettingsHelper.Settings.Clone() as Settings;
            FormatSettingName(MenuId.VideoSetup, "Resolution", "Resolution");
            FormatSettingName(MenuId.GameSetup, "LostFocus", nameof(TempSettings.PauseOnLostFocus));
            FormatSettingName(MenuId.GameSetup, "Language", nameof(TempSettings.Language));
            FormatSettingName(MenuId.VideoSetup, "Fullscreen", nameof(TempSettings.Fullscreen));
            FormatSettingName(MenuId.VideoSetup, "VSync", nameof(TempSettings.VSync));
            FormatSettingName(MenuId.AudioSetup, "Sound", nameof(TempSettings.SoundVolume));
            FormatSettingName(MenuId.AudioSetup, "Music", nameof(TempSettings.MusicVolume));
        }

        private void AttachManyListeners()
        {
            void AttachListener(MenuId menu, string button, string method, params object[] binds)
            {
                Button btn = Menus.GetChildOrNull<VBoxContainer>((int) menu).GetNodeOrNull<Button>(button);
                btn.Connect("pressed", this, method, new Godot.Collections.Array(binds));
                btn.Connect("pressed", this, nameof(PlayConfirmOnClick));
            }

            AttachListener(MenuId.Root, "Resume", nameof(ReturnToGame));
            AttachListener(MenuId.Root, "Start", nameof(ChangeMenu), MenuId.StartNewGame, 0);
            AttachListener(MenuId.Root, "Options", nameof(ChangeMenu), MenuId.Options, 0);
            AttachListener(MenuId.Root, "Quit", nameof(ChangeMenu), MenuId.QuitGame, 0);

            AttachListener(MenuId.StartNewGame, "New", nameof(ChangeMenu), MenuId.SelectSlot, SlotMode.New);
            AttachListener(MenuId.StartNewGame, "Load", nameof(ChangeMenu), MenuId.SelectSlot, SlotMode.Load);
            AttachListener(MenuId.StartNewGame, "Erase", nameof(ChangeMenu), MenuId.SelectSlot, SlotMode.Erase);

            AttachListener(MenuId.SelectSlot, "Slot 1", nameof(SlotSelected), 0);
            AttachListener(MenuId.SelectSlot, "Slot 2", nameof(SlotSelected), 1);
            AttachListener(MenuId.SelectSlot, "Slot 3", nameof(SlotSelected), 2);

            AttachListener(MenuId.Options, "HowToPlay", nameof(ChangeMenu), MenuId.HowToPlay, SlotMode.None);
            AttachListener(MenuId.Options, "GameSetup", nameof(ChangeMenu), MenuId.GameSetup, SlotMode.None);
            AttachListener(MenuId.Options, "VideoSetup", nameof(ChangeMenu), MenuId.VideoSetup, SlotMode.None);
            AttachListener(MenuId.Options, "AudioSetup", nameof(ChangeMenu), MenuId.AudioSetup, SlotMode.None);

            AttachListener(MenuId.GameSetup, "Language", nameof(ChangeMenu), MenuId.Language, SlotMode.None);
            AttachListener(MenuId.GameSetup, "LostFocus", nameof(ChangeSettings),
                nameof(TempSettings.PauseOnLostFocus));
            AttachListener(MenuId.GameSetup, "Reset", nameof(ResetToDefaults));

            AttachListener(MenuId.VideoSetup, "Resolution", nameof(ChangeSettings), "Resolution");
            AttachListener(MenuId.VideoSetup, "Fullscreen", nameof(ChangeSettings), nameof(TempSettings.Fullscreen));
            AttachListener(MenuId.VideoSetup, "VSync", nameof(ChangeSettings), nameof(TempSettings.VSync));
            AttachListener(MenuId.VideoSetup, "Reset", nameof(ResetToDefaults));

            AttachListener(MenuId.AudioSetup, "Sound", nameof(ChangeSettings), nameof(TempSettings.SoundVolume));
            AttachListener(MenuId.AudioSetup, "Music", nameof(ChangeSettings), nameof(TempSettings.MusicVolume));
            AttachListener(MenuId.AudioSetup, "Reset", nameof(ResetToDefaults));
            
            AttachListener(MenuId.Language, "English", nameof(SelectLanguage), "en");
            AttachListener(MenuId.Language, "French", nameof(SelectLanguage), "fr");
            AttachListener(MenuId.Language, "Italian", nameof(SelectLanguage), "it");
            AttachListener(MenuId.Language, "German", nameof(SelectLanguage), "de");
            AttachListener(MenuId.Language, "Spanish", nameof(SelectLanguage), "es");
            AttachListener(MenuId.Language, "Portuguese", nameof(SelectLanguage), "pt");
        }

        private void FormatSettingName(MenuId id, string button, string setting)
        {
            object value;
            if (setting == "Resolution")
            {
                value = $"{TempSettings.Width} x {TempSettings.Height}";
            }
            else
            {
                value = TempSettings.GetType().GetProperty(setting)?.GetValue(TempSettings, null);
                if (value is bool boolean)
                    value = (boolean) ? Tr("On") : Tr("Off");
                else if (value is float single)
                    value = single;
                else if (value is string str)
                    value = Tr("Language" + TranslationServer.GetLocaleName(str));
            }

            Button btn = Menus.GetChildOrNull<VBoxContainer>((int) id).GetNodeOrNull<Button>(button);
            if (!_formattedStrings.ContainsKey(button))
            {
                _formattedStrings[button] = Tr(btn.Text);
            }
            btn.Text = string.Format(_formattedStrings[button], value).ToUpper();
        }

        private void ReadSlotInfo()
        {
            for (int i = 0; i < 3; i++)
            {
                Button btn = Menus.GetChildOrNull<VBoxContainer>((int) MenuId.SelectSlot).GetChildOrNull<Button>(i);
                if (btn != null)
                {
                    string path = $"user://SaveSlot{i}.json";
                    if (FileOperations.TryLoadJson(path, out SaveData saveData))
                    {
                        TimeSpan playTime = TimeSpan.FromTicks(saveData.PlayTime);
                        btn.Text = Tr("SaveSlotPrefix") + $" {i + 1}: {playTime:hh\\:mm\\:ss}";
                    }
                    else
                    {
                        btn.Text = Tr("NewSlot");
                    }
                }
            }
        }

        private void ChangeSettings(string setting)
        {
            int rightLeft = (int) Input.GetActionStrength(UiAction.Right) -
                            (int) Input.GetActionStrength(UiAction.Left);
            
            if (rightLeft != 0)
            {
                object value;
                object original;
                if (setting == "Resolution")
                {
                    Tuple<int, int> value1 = new Tuple<int, int>(TempSettings.Width, TempSettings.Height);
                    Tuple<int, int> original1 = new Tuple<int, int>(SettingsHelper.Settings.Width, SettingsHelper.Settings.Height);
                    
                    int idx = Array.IndexOf(SettingsHelper.ScreenResolutions, value1);
                    idx = Mathf.PosMod(idx - rightLeft, SettingsHelper.ScreenResolutions.Length);
                    
                    value1 = SettingsHelper.ScreenResolutions[idx];
                    TempSettings.Width = value1.Item1;
                    TempSettings.Height = value1.Item2;
                    
                    value = value1;
                    original = original1;
                }
                else
                {
                    value = TempSettings.GetType().GetProperty(setting)?.GetValue(TempSettings, null);
                    original = SettingsHelper.Settings.GetType().GetProperty(setting)
                        ?.GetValue(SettingsHelper.Settings, null);
                    
                    value = value switch
                    {
                        bool boolean => !boolean,
                        float single => Mathz.Clamp01(single + rightLeft * 0.01f),
                        int integer => integer + rightLeft,
                        _ => value
                    };
                    
                    TempSettings.GetType().GetProperty(setting)?.SetValue(TempSettings, value);
                }
                
                if (rightLeft > 0) SliderInc.Play();
                if (rightLeft < 0) SliderDec.Play();

                ConfirmHint.Visible = !original!.Equals(value);
                FormatSettingName(CurrentMenu, CurrentMenuButtons[_focusedButtonId].Name, setting);
            }
        }

        private void SelectLanguage(string locale)
        {
            if (TranslationServer.GetLoadedLocales().Contains(locale))
            {
                TranslationServer.SetLocale(locale);
                TempSettings.Language = locale;
                ApplySettings();
                ChangeMenu(PreviousMenu);
            }
            else
            {
                string name = TranslationServer.GetLocaleName(locale);
                string message = $"Sorry, i do not speak {locale}" + (string.IsNullOrEmpty(name) ? "" : $" / {name})");
                GD.PushError(message);
            }
        }

        private void ApplySettings()
        {
            SettingsHelper.Settings = TempSettings.Clone() as Settings;
            SettingsHelper.Apply(this);
            SettingsHelper.Save();
        }

        private async void ChangeMenu(MenuId id, SlotMode slotMode = 0)
        {
            VBoxContainer nextInstance = Menus.GetChildOrNull<VBoxContainer>((int) id);
            if (nextInstance is null)
                return;

            await PopOut();
            CurrentMenu = id;
            List<Control> actualMenus = Menus.GetChildrenList<Control>();
            foreach (Control menu in actualMenus)
            {
                menu.Visible = menu.Name == nextInstance.Name;
            }

            if (id == MenuId.Root)
            {
                Header.Text = Tr("PauseMenuTitle");
                Header.Visible = Navigation.Visible = false;
                nextInstance.GetNodeOrNull<Button>("Resume").Visible = _inGame;
                _focusedButtonId = _inGame ? 0 : 1;
            }
            else
            {
                if (slotMode == SlotMode.None)
                {
                    Header.Text = nextInstance.Name;
                }
                else
                {
                    string symbol = slotMode switch
                    {
                        SlotMode.New => "+",
                        SlotMode.Erase => "-",
                        _ => "?",
                    };
                    Header.Text = string.Join(" ", Tr(nextInstance.Name), $"({symbol})").ToUpper();
                }

                if (id == MenuId.Language)
                    Header.Visible = false;
                else
                    Header.Visible = Navigation.Visible = true;
                ConfirmHint.Visible = ChangeHint.Visible = false;
                _focusedButtonId = 0;
            }

            switch (CurrentMenu)
            {
                case MenuId.AudioSetup:
                case MenuId.VideoSetup:
                case MenuId.GameSetup:
                    ReadSettings();
                    break;

                case MenuId.SelectSlot:
                    ReadSlotInfo();
                    _actionOnSlot = slotMode;
                    break;

                case MenuId.QuitGame:
                    ConfirmHint.Visible = true;
                    break;
            }

            await PopIn();
            if (CurrentMenuButtons.Count > 0)
                CurrentMenuButtons[_focusedButtonId].CallDeferred("grab_focus");
        }

        private void PlayConfirmOnClick()
        {
            if (FzInput.Event is InputEventMouseButton)
                Confirm.Play();
        }

        public override void _Process(float delta)
        {
            if (CurrentMenuButtons.Count > 0)
            {
                Button btn = CurrentMenuButtons[_focusedButtonId];
                _selectedIsSlider = _formattedStrings.TryGetValue(btn.Name, out string str) && str.Contains("{0") &&
                                    !str.Contains("LANGUAGE");
                ChangeHint.Visible = _selectedIsSlider;
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (DebugManager.ConsoleShowed) return;
            FzInput.Event = @event;

            int rightLeft = (int) Input.GetActionStrength(UiAction.Right) -
                            (int) Input.GetActionStrength(UiAction.Left);
            int upDown = (int) Input.GetActionStrength(UiAction.Down) -
                         (int) Input.GetActionStrength(UiAction.Up);

            bool isAccepted = Input.IsActionJustPressed(UiAction.Accept);
            bool isCanceled = Input.IsActionJustPressed(UiAction.Cancel);

            int begin = (CurrentMenu == MenuId.Root && !_inGame) ? 1 : 0;
            if (CurrentMenuButtons.Count > 0 && upDown != 0)
            {
                int nextId = Mathf.Clamp(_focusedButtonId + upDown, begin, CurrentMenuButtons.Count - 1);
                if (_focusedButtonId >= begin && _focusedButtonId <= CurrentMenuButtons.Count - 1)
                {
                    _focusedButtonId = nextId;
                    CurrentMenuButtons[_focusedButtonId].CallDeferred("grab_focus");

                    if (upDown > 0)
                        CursorUp.Play();
                    if (upDown < 0)
                        CursorDown.Play();
                }
            }

            if (CurrentMenuButtons.Count > 0)
            {
                if (isAccepted || (rightLeft != 0 && _selectedIsSlider))
                {
                    CurrentMenuButtons[_focusedButtonId].EmitSignal("pressed");
                    if (!_selectedIsSlider) Confirm.Play();
                }
            }

            if (isAccepted && !Animation.IsPlaying())
            {
                if (CurrentMenu == MenuId.QuitGame)
                {
                    QuitGame();
                }
                else if (ConfirmHint.Visible &&
                         CurrentMenu.In(MenuId.GameSetup, MenuId.AudioSetup, MenuId.VideoSetup))
                {
                    ApplySettings();
                    ConfirmHint.Visible = false;
                }
            }

            if (isCanceled && PreviousMenu != MenuId.None && !Animation.IsPlaying())
            {
                if (PreviousMenu != MenuId.None)
                {
                    Cancel.Play();
                    ChangeMenu(PreviousMenu);
                }
                else ReturnToGame();
            }
        }
    }
}