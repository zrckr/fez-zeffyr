using Godot;
using Zeffyr.Components;
using Zeffyr.Structure.Physics;

namespace Zeffyr.Structure
{
    public interface IPlayerEntity : IPhysicsEntity
    {
        float DefaultSpeed { get; }

        float LightFactor { get; }

        float HeavyFactor { get; }

        bool Visible { get; set; }

        bool IgnoreDiePanic { get; set; }

        bool IsHatHidden { get; set; }

        bool CanDoubleJump { get; set; }

        ActionType Action { get; set; }

        ActionType LastAction { get; }

        ActionType NextAction { get; set; }

        CollisionObject HeldBody { get; set; }

        Pickup CarriedBody { get; set; }

        Pickup PushedBody { get; set; }

        Direction FacingDirection { get; set; }

        RespawnHelper Respawn { get; }

        Axes Axes { get; }
        
        Vector3 Rotation { get; set; }

        float BlinkSpeed { get; set; }
        
        float Opacity { get; set; }
        
        float Brightness { get; set; }

        float AirTime { get; set; }

        bool Alive { get; }

        void Reset();

        object Call(string method, params object[] args);

        void SetPhysicsProcess(bool enable);

        ChangeLevelArea ChangeArea { get; set; }
        
        Spatial CollectedTreasure { get; set; }
    }
}