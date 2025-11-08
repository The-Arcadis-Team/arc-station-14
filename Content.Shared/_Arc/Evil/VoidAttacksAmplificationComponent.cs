using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Arc.Evil;

[RegisterComponent, NetworkedComponent]

// STRIKE ME DOWN, ISHMAEL!
public sealed partial class VoidAttacksAmplificationComponent : Component
{
    [DataField]
    public int FlingDistance = 2;

    [DataField]
    public float DamageMultiplier = 15f; // dear god.

    [DataField]
    public string AttackPopup = "void-attack-hit";
}
