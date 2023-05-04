using Godot;

namespace Zeffyr.Managers
{
    public class MusicManager : AudioStreamPlayer
    {
        public int MusicBus => AudioServer.GetBusIndex("Music");
        
        public int SoundBus => AudioServer.GetBusIndex("Sounds");
        
        public int MasterBus => AudioServer.GetBusIndex("Master");

        public float MusicVolume
        {
            get => GD.Db2Linear(AudioServer.GetBusVolumeDb(MusicBus));
            set => AudioServer.SetBusVolumeDb(MusicBus, GD.Linear2Db(value));
        }
        
        public float SoundVolume
        {
            get => GD.Db2Linear(AudioServer.GetBusVolumeDb(SoundBus));
            set => AudioServer.SetBusVolumeDb(SoundBus, GD.Linear2Db(value));
        }
        
        public float MasterVolume
        {
            get => GD.Db2Linear(AudioServer.GetBusVolumeDb(MasterBus));
            set => AudioServer.SetBusVolumeDb(MasterBus, GD.Linear2Db(value));
        }

        private readonly Tween _tween = new Tween()
        {
            Name = nameof(Tween),
            Repeat = false,
        };

        public override void _Ready()
        {
            this.InjectNodes();
            this.AddChild(_tween);
            this.Bus = "Music";
        }

        public void PlayNewSong(AudioStream sample, float fadeDuration)
        {
            if (Playing)
            {
                FadeOutAndRemove(fadeDuration);
            }
            Stream = sample;
            if (Stream is not null && Stream.ResourcePath != sample.ResourcePath)
            {
                Play();
            }
        }
        
        private async void FadeOutAndRemove(float fadeDuration)
        {
            float initial = GD.Linear2Db(1f);
            float final = GD.Linear2Db(0f);
            _tween.InterpolateProperty(this, "volume_db", initial, final, fadeDuration, 
                Tween.TransitionType.Sine, Tween.EaseType.Out);
            _tween.Start();
            
            await ToSignal(_tween, "tween_all_completed");
            Stream = null;
            Stop();
        }

        public void FadeVolume(float from, float to, float seconds)
        {
            float initial = GD.Linear2Db(from);
            float final = GD.Linear2Db(to);
            _tween.InterpolateProperty(this, "volume_db", initial, final, seconds, 
                Tween.TransitionType.Linear, Tween.EaseType.Out);
            _tween.Start();
        }
    }
}