using System;
using Godot;
using System.Collections.Generic;
using System.Globalization;

namespace Zeffyr.Managers
{
    public class DebuggingBag : CanvasLayer
    {
        [Node("Container")] protected MarginContainer Container;
        [Node("Container/VBox/Label")] protected Label Label;
        
        [RootNode] protected GameStateManager GameState;
        [RootNode] protected TimeManager TimeManager;

        private Dictionary<string, List<string>> _messages = new Dictionary<string, List<string>>();

        public override void _Ready()
        {
            this.InjectNodes();
            this.Visible = OS.IsDebugBuild();
            PauseMode = PauseModeEnum.Process;
        }

        public bool Visible
        {
            get => Container.Visible;
            set => Container.Visible = value;
        }

        public string Text => Label.Text;

        public void Add(string tag, params object[] messages)
        {
            if (!_messages.ContainsKey(tag))
            {
                _messages[tag] = new List<string>();
            }
            _messages[tag].Add(string.Join(" ", messages));
        }

        public override void _Process(float delta)
        {
            Label.Text = $"BUILD: {FormBuildDate()}\n";
            Label.Text += $"SCENE: {GetTree().CurrentScene?.Name}\n";
            Label.Text += $"FPS: {Engine.GetFramesPerSecond()}\n";
            Label.Text += $"SPEED: {Engine.TimeScale}\n";
            Label.Text += $"TIME: {TimeManager.CurrentTime.ToString("HH:mm")}\n";
            Label.Text += $"PAUSED: {GetTree().Paused}\n";

            foreach (string key in _messages.Keys)
            {
                List<string> value = _messages[key];
                Label.Text += "\n";
                foreach (string str in value)
                    Label.Text += $"{str}\n";
                value.Clear();
            }
        }
        
        private string FormBuildDate()
        {
            /*
             * Format - YYwWW_dH
             * "YY" is the two-digit year,
             * "w" stands for "week",
             * "WW" is the two-digit week number within the year,
             * "d" is number of the day of the week.
             * "H" is hour ASCII char
             */
            const string buildLetters = "NOPQRSTVWXYZABCDEFGHJKLM";
            
            DateTime time = DateTime.Now;
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);

            string year = time.Date.ToString("yy");
            int weekOfYear =
                CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek,
                    DayOfWeek.Monday);

            int dayOfWeek = (day == DayOfWeek.Sunday) ? 7 : (int) time.DayOfWeek;
            char hours = buildLetters[(int) time.TimeOfDay.TotalHours];

            return $"{year}w{weekOfYear:00}_{dayOfWeek}{hours}";
        }
    }
}