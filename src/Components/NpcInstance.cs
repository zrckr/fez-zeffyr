using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using Zeffyr.Components.Actions;
using Zeffyr.Structure;
using Zeffyr.Structure.Input;
using Zeffyr.Structure.Physics;
using Zeffyr.Tools;
using Random = Zeffyr.Tools.Random;

namespace Zeffyr.Components
{
    public class NpcInstance : AnimatedSprite3D
    {
        [Export] public float WalkSpeed = 1.5f;

        [Export] public bool AvoidsGomez;

        [Export] public bool RandomizeSpeech;

        [Export] public bool SayFirstSpeechLineOnce;

        [Export] public string[] SpeechTags = new string[0];

        [Export] public Vector3 DestinationOffset;

        [Export] public Dictionary<string, AudioStream> AnimationSound = new Dictionary<string, AudioStream>()
        {
            ["idle"] = null,
            ["idle2"] = null,
            ["idle3"] = null,
            ["turn"] = null,
            ["walk"] = null,
            ["talk"] = null,
        };

        public NpcAction CurrentAction { get; set; }

        public Direction FacingDirection
        {
            get => _direction;
            set
            {
                if (_direction.GetOpposite() == value)
                {
                    _direction = value;
                    FlipH = FacingDirection == Direction.Left;
                }
            }
        }

        public string CurrentSpeechLine { get; private set; }

        public Vector3 Origin
        {
            get => GlobalTransform.origin;
            set
            {
                Transform transform = GlobalTransform;
                transform.origin = value;
                GlobalTransform = transform;
            }
        }
        
        private bool _canIdle;
        private bool _canIdle2;
        private bool _canIdle3;
        private bool _canTurn;
        private bool _canWalk;
        private bool _canTalk;
        
        private bool _saidFirstLine;
        private int _speechLineIdx;
        private bool _waitToSpeak;

        private float _timeUntilActionChange;
        private float _timeSinceActionChange;
        
        private bool _enteredTree;
        private float _walkStep;
        private float _walkedDistance;
        private Vector3 _oldOrigin;
        private Quat _oldRotation;
        private Direction _oldDirection;
        private SpeechBubble _speechBubble;
        private Direction _direction;
        private AudioStreamPlayer _audio;

        [CameraHelper(typeof(GameCamera))]
        private GameCamera GameCamera { get; set; } = default;

        private Gomez Gomez => (GetTree().CurrentScene as Level)?.Gomez;

        private Axes CameraAxes => new Axes(GameCamera.Orthogonal.GetBasis());

        public override void _Ready()
        {
            this.InjectNodes();
            Playing = true;
            GameCamera.Connect(nameof(BaseCamera.Rotating), this, nameof(UpdateRotation));
            GameCamera.Connect(nameof(BaseCamera.Rotated), this, nameof(UpdatePath));

            _audio = new AudioStreamPlayer()
            {
                Bus = "Sounds",
            };
            AddChild(_audio);
            
            _canIdle = Frames.HasAnimation("idle");
            _canIdle2 = Frames.HasAnimation("idle2");
            _canIdle3 = Frames.HasAnimation("idle3");
            _canTurn = Frames.HasAnimation("turn");
            _canWalk = Frames.HasAnimation("walk");
            _canTalk = Frames.HasAnimation("talk");
            
            if (!_enteredTree)
            {
                _walkStep = Random.Unit();
                _oldOrigin = Origin;
                FacingDirection = Direction.Right;
                UpdatePath();
                Walk(0f);   // Set initial random offset for origin
                ToggleAction();
                UpdateRotation();
                _enteredTree = true;
            }
        }

        public override void _PhysicsProcess(float delta)
        {
            if (CurrentAction.AllowsRandomChange())
            {
                _timeSinceActionChange += delta;
                if (_timeSinceActionChange > _timeUntilActionChange)
                    ToggleAction();
            }
            else if (!CurrentAction.Loops() && !Playing && CurrentAction != NpcAction.Hide)
            {
                ToggleAction();
            }

            if (CurrentAction != NpcAction.Talk)
            {
                if (_canTalk && (SpeechTags.Length > 0 || !string.IsNullOrEmpty(CurrentSpeechLine)))
                    TryTalk();
                if (CurrentAction == NpcAction.Walk)
                    Walk(delta);
            }
            else if (TryStopTalk())
                ToggleAction();
        }

        private void Walk(float delta)
        {
            float target = (FacingDirection.Sign() * CameraAxes.Right).Dot(DestinationOffset.Sign());
            float current = Mathf.IsZeroApprox(_walkedDistance) ? 1.0f : _walkedDistance;
            _walkStep += (target / current * delta) * WalkSpeed;
            
            if (_walkStep > 1f || _walkStep < 0f)
            {
                _walkStep = Mathz.Clamp01(_walkStep);
                ToggleAction();
            }
            else
            {
                _walkStep = Mathz.Clamp01(_walkStep);
                Origin = _oldOrigin.LinearInterpolate(_oldOrigin + DestinationOffset, _walkStep);
            }
        }

        private async void Talk()
        {
            bool wasTalking = CurrentAction == NpcAction.Talk;
            if (string.IsNullOrEmpty(CurrentSpeechLine))
            {
                if (SpeechTags.Length <= 1 || SayFirstSpeechLineOnce && !_saidFirstLine)
                {
                    CurrentSpeechLine = SpeechTags.FirstOrDefault();
                }
                else
                {
                    do
                    {
                        if (RandomizeSpeech)
                        {
                            CurrentSpeechLine = Random.Choose(SpeechTags);
                        }
                        else
                        {
                            CurrentSpeechLine = SpeechTags[_speechLineIdx];
                            _speechLineIdx = (_speechLineIdx + 1) % SpeechTags.Length;
                        }
                    } while (SayFirstSpeechLineOnce && _saidFirstLine && CurrentSpeechLine == SpeechTags[0]);
                }
                _saidFirstLine = true;
            }
            
            Vector3 diff = Gomez.Origin - Origin;
            float x = diff.Dot(CameraAxes.XMask);
            
            if (Mathf.Abs(x) < 1f && !wasTalking)
            {
                WalkTo walkTo = Gomez.GetNode<WalkTo>("Actions/WalkTo");
                walkTo.NextOrigin = () => Origin * CameraAxes.XzMask + 
                                          Gomez.Origin * CameraAxes.YMask +
                                          Mathf.Sign(x) * 1.125f * CameraAxes.Right;
                walkTo.NextAction = ActionType.ReadListen;
                Gomez.Action = ActionType.WalkTo;
                _waitToSpeak = true;
            }
            else
            {
                Gomez.Action = ActionType.ReadListen;
            }

            FacingDirection = diff.Dot(CameraAxes.Right).GetDirectionFrom();
            Gomez.FacingDirection = FacingDirection.GetOpposite();
            if (Gomez.Action == ActionType.WalkTo)
            {
                CurrentAction = _canIdle ? NpcAction.Idle : NpcAction.Talk;
                UpdateAction();
                await ToSignal(Gomez.Signals, nameof(ActionSignals.WalkedTo));
            }

            _waitToSpeak = false;
            _speechBubble = SpeechBubble.CreateInstance();
            _speechBubble.SetText(CurrentSpeechLine, Origin + CameraAxes.YMask, 1.5f);
            ReadListen.SpeechInstance = _speechBubble;
            
            CurrentAction = NpcAction.Talk;
            UpdateAction();
        }

        private void TryTalk()
        {
            switch (Gomez.Action)
            {
                case ActionType.Idle:
                case ActionType.Walk:
                case ActionType.Run:
                case ActionType.Slide:
                    if (!Gomez.InBackground && _speechBubble is null && string.IsNullOrEmpty(CurrentSpeechLine) &&
                        Input.IsActionJustPressed(InputAction.TalkCancel))
                    {
                        var space = GetWorld().DirectSpaceState;
                        if (space.CastRect(GlobalTransform, Vector3.One, PhysicsLayer.Player).First != null)
                            Talk();
                    }
                    break;
            }
        }

        private bool TryStopTalk()
        {
            bool flag = _speechBubble is null || !IsInstanceValid(_speechBubble) && !_waitToSpeak;
            if (flag)
            {
                _waitToSpeak = false;
                if (string.IsNullOrEmpty(CurrentSpeechLine) && !RandomizeSpeech && (!SayFirstSpeechLineOnce && _speechLineIdx != 0))
                {
                    Talk();
                    return false;
                }
                Gomez.Action = ActionType.Idle;
                CurrentSpeechLine = null;
                _speechBubble = null;
            }
            return flag;
        }

        private void ToggleAction()
        {
            NpcAction oldAction = CurrentAction;
            if (_enteredTree)
                RandomizeAction();
            else
                CurrentAction = _canIdle ? NpcAction.Idle : NpcAction.Walk;
            
            _timeUntilActionChange = Random.Range(2f, 5f);
            _timeSinceActionChange = 0f;
            if (!_enteredTree || oldAction != CurrentAction)
                UpdateAction();
        }

        private void RandomizeAction()
        {
            switch (CurrentAction)
            {
                case NpcAction.Turn:
                    Turn();
                    break;
                case NpcAction.Burrow:
                    QueueFree();
                    break;
                default:
                    if ((Random.Probability(0.5f) || !_canWalk) && _canIdle)
                    {
                        if (_canWalk || Random.Probability(0.5f))
                        {
                            ChooseIdle();
                            break;
                        }
                        if (_canTurn)
                        {
                            CurrentAction = NpcAction.Turn;
                            break;
                        }
                        Turn();
                        break;
                    }
                    if (!_canWalk)
                        throw new InvalidOperationException("This NPC can't walk or idle!");

                    if (Mathf.IsEqualApprox(_walkStep, 1f) || Mathf.IsZeroApprox(_walkStep))
                    {
                        if (_canIdle && Random.Probability(0.5f))
                        {
                            ChooseIdle();
                            break;
                        }
                        if (_canTurn)
                        {
                            CurrentAction = NpcAction.Turn;
                            break;
                        }
                        Turn();
                        break;
                    }

                    if (_canTurn && Random.Probability(0.5f))
                    {
                        CurrentAction = NpcAction.Turn;
                        break;
                    }
                    
                    CurrentAction = NpcAction.Walk;
                    break;
            }
        }

        private void Turn()
        {
            FacingDirection = FacingDirection.GetOpposite();
            CurrentAction = _canWalk ? NpcAction.Walk : NpcAction.Idle;
            UpdateRotation();
        }

        private void ChooseIdle()
        {
            if (CurrentAction.IsSpecialIdle())
            {
                CurrentAction = NpcAction.Idle;
            }
            else
            {
                float num1 = Random.Unit();
                float num2 = (1f + Convert.ToInt32(_canIdle2) + Convert.ToInt32(_canIdle3));

                if (num1 < 1.0f / num2)
                {
                    CurrentAction = NpcAction.Idle;
                }
                else if (num2 > 1.0f && num1 < 2.0f / num2)
                {
                    CurrentAction = _canIdle2 ? NpcAction.Idle2 : NpcAction.Idle3;
                }
                else if (num2 > 2.0f && num1 < 3.0f / num2)
                {
                    CurrentAction = NpcAction.Idle3;
                }
            }
        }

        private void UpdateAction()
        {
            string animationName = Enum.GetName(typeof(NpcAction), CurrentAction);
            if (!string.IsNullOrEmpty(animationName))
            {
                Animation = animationName.ToLower();
                AudioStream audio = AnimationSound[Animation];
                if (audio != null && _enteredTree)
                {
                    _audio.Stream = audio;
                    _audio.PitchScale = Random.Range(0.9f, 1.1f);
                    _audio.Play();
                }
            }
        }
        
        // ReSharper disable once UnusedParameter.Local
        private void UpdateRotation(float step = 0f)
        {
            Quat rotation = new Quat(GameCamera.GlobalTransform.basis).Normalized();
            if (_oldRotation != rotation || _oldDirection != FacingDirection)
            {
                _oldRotation = rotation;
                _oldDirection = FacingDirection;
                Rotation = rotation.GetEuler();
            }
        }

        private void UpdatePath()
        {
            float oldDistance = _walkedDistance;
            _walkedDistance = Mathf.Abs(DestinationOffset.Dot(CameraAxes.XMask));
            if (Mathf.IsZeroApprox(_walkedDistance))
                _walkedDistance = oldDistance;
        }
    }
}