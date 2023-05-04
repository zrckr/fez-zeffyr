using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using Zeffyr.Structure.Input;
using Zeffyr.Structure.Physics;
using Array = Godot.Collections.Array;

namespace Zeffyr.Components
{
    public class CodeArea : Area
    {
        [Signal] 
        public delegate void CodeAccepted();
        
        [Export]
        public readonly string[] CodePattern;

        [CameraHelper(typeof(GameCamera))]
        public GameCamera GameCamera;

        public readonly List<string> FetchedActions = new List<string>();
        
        private bool _fetchCheck;
        private float _sinceNoInput;
        private const float NoInputTime = 5f;

        public override void _Ready()
        {
            if (CodePattern.Length > 16 || CodePattern is null || CodePattern.Length <= 0)
                throw new ArgumentOutOfRangeException(nameof(CodePattern),
                    "Invalid code pattern. The length must be between 1 and 16.");
            
            for (int i = 1; i <= CodePattern.Length; i++)
            {
                string code = CodePattern[i-1];
                if (!string.IsNullOrEmpty(code) && !(InputMap.HasAction(code) || code.Contains(UiAction.Prefix)))
                    throw new ArgumentException($"Code №{i} ({code}) is not defined in InputMap.");
            }

            this.InjectNodes();
            this.Connect("body_entered", this, nameof(OnBodyDetected),new Array(true));
            this.Connect("body_exited", this, nameof(OnBodyDetected),new Array(false));
            this.ShapeOwnerUpdateDepth(0);
        }

        private void OnBodyDetected(Node body, bool entered)
        {
            if (body is Gomez)
            {
                SetProcess(entered);
                FetchedActions.Clear();
                _sinceNoInput = 0f;
            }
        }

        public override void _Process(float delta)
        {
            _sinceNoInput += delta;
            _fetchCheck = FetchInputActions();

            if (_fetchCheck)
            {
                TestCodePattern();
            }
            else if (_sinceNoInput > NoInputTime)
            {
                _sinceNoInput = 0f;
                FetchedActions.Clear();
            }
        }

        private bool FetchInputActions()
        {
            string pressed = InputMap.GetActions()
            .Cast<string>()
            .Where(a => !a.Contains(UiAction.Prefix))
            .FirstOrDefault(a => Input.IsActionJustPressed(a));

            if (pressed != null)
            {
                _sinceNoInput = 0f;
                FetchedActions.Add(pressed);
                if (FetchedActions.Count > 16)
                    FetchedActions.RemoveAt(0);
                return true;
            }
            return false;
        }
        
        private void TestCodePattern()
        {
            string pattern = string.Join(" ", CodePattern);
            string input = string.Join(" ", FetchedActions);
            if (input.Contains(pattern))
            {
                FetchedActions.Clear();
                EmitSignal(nameof(CodeAccepted));
            }
        }
    }
}