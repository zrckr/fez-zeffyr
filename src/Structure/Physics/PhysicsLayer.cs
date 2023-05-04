using System;

namespace Zeffyr.Structure.Physics
{
    [Flags]
    public enum PhysicsLayer : uint
    {
        None =               0,     // a.k.a Immaterial
        AllSides =      1 << 0,
        TopOnly =       1 << 1,
        Background =    1 << 2,     // a.k.a None 
        Player =        1 << 3,
        Actors =        1 << 4,
        Ladder =        1 << 5,
        Vine =          1 << 6,
        Bounce =        1 << 7,
        Deadly =        1 << 8,
        MapNode =       1 << 9,
        
        // Helpers
        Solid =         AllSides | TopOnly | Background,
        Climbable =     Ladder | Vine,
    }
}