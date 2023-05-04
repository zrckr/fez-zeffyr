using Godot;
using Zeffyr.Structure;

namespace Zeffyr.Components
{
    public class ChangeLevelArea : Area
    {
        [Export]
        public readonly string NextLevel;
        
        [Export(PropertyHint.Range, "0,255,1")]
        public int VolumeId { get; private set; }

        [Export]
        public readonly bool DoSpin;

        [Export]
        public readonly bool KeyRequired;

        [Export]
        public readonly Orthogonal ViewAfterWarp = Orthogonal.Front;

        public Spatial Door => this.GetNodeOrNull<Spatial>("Door");

        public Spatial BitDoor => this.GetNodeOrNull<Spatial>("BitDoor");

        public Spatial Warp => this.GetNodeOrNull<Spatial>("Warp");

        public Spatial Change => this.GetNodeOrNull<Spatial>("Change");
        
        public override void _Ready()
        {
            if (Change != null)
                Change.Visible = !OS.IsDebugBuild();
        }
    }
}