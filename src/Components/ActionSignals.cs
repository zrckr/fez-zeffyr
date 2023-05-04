using Godot;

namespace Zeffyr.Components
{
    public class ActionSignals : Node
    {
        [Signal] public delegate void EnteredDoor();

        [Signal] public delegate void Jumped();

        [Signal] public delegate void ClimbedLadder();

        [Signal] public delegate void ClimbedVine();

        [Signal] public delegate void LookedAround();

        [Signal] public delegate void LiftedObject();

        [Signal] public delegate void ThrewObject();

        [Signal] public delegate void OpenedMenuCube();

        [Signal] public delegate void ReadHeard();

        [Signal] public delegate void GrabbedLedge();

        [Signal] public delegate void DroppedObject();

        [Signal] public delegate void DroppedLedge();

        [Signal] public delegate void Hoisted();

        [Signal] public delegate void ClimbedOverLadder();

        [Signal] public delegate void DroppedFromLadder();

        [Signal] public delegate void CollectedSmallCube();

        [Signal] public delegate void CollectedBigCube();

        [Signal] public delegate void OpenedTreasure();

        [Signal] public delegate void CollectedAntiCube();

        [Signal] public delegate void Landed();

        [Signal] public delegate void EnterFirstPersonMode();
        
        [Signal] public delegate void ExitFirstPersonMode();

        [Signal] public delegate void WalkedTo();
    }
}