using Godot;
using System.Collections.Generic;
using Zeffyr.Managers;

namespace Zeffyr.Components.GUI
{
    public class InGameHud : MarginContainer
    {
        [RootNode]
        public GameStateManager GameState;
        
        [Node("VBox/Big Cubes")]
        public HBoxContainer BigCubes;
        
        [Node("VBox/Small Cubes")]
        public HBoxContainer SmallCubes;
        
        [Node("VBox/Anti Cubes")]
        public HBoxContainer AntiCubes;
        
        [Node("VBox/Keys")]
        public HBoxContainer Keys;
        
        [Node]
        public AnimationPlayer Showing;
        
        [Node]
        public AnimationPlayer Saving;

        public override void _Ready()
        {
            this.InjectNodes();
            Visible = false;
            GameState.Connect(nameof(GameStateManager.OnHudChanged), this, nameof(ShowNumbers));
            GameState.Connect(nameof(GameStateManager.OnSaved), this, nameof(ShowSaving));
        }

        public void ShowSaving()
        {
            if (!Saving.IsPlaying())
                Saving.Play("default");
            else
                Saving.Seek(0.3f, true);
        }

        public void ShowNumbers()
        {
            BigCubes.GetNodeOrNull<Label>("Count").Text = GameState.SaveData.BigCubes.ToString();
            AntiCubes.GetNodeOrNull<Label>("Count").Text = GameState.SaveData.AntiCubes.ToString();
            Keys.GetNodeOrNull<Label>("Count").Text = GameState.SaveData.Keys.ToString();
            
            SmallCubes.Visible = GameState.SaveData.SmallCubes != 0;
            if (SmallCubes.Visible)
            {
                string number = GameState.SaveData.SmallCubes.ToString();
                List<TextureRect> icons = SmallCubes.GetNodeOrNull("Icons").GetChildrenList<TextureRect>();
                
                SmallCubes.GetNodeOrNull<Label>("Count").Text = number;
                foreach (TextureRect icon in icons)
                {
                    icon.Visible = (icon.Name == number);
                }
            }
            
            if (!Showing.IsPlaying())
                Showing.Play("default");
            else
                Showing.Seek(0.25f, true);
        }
    }
}