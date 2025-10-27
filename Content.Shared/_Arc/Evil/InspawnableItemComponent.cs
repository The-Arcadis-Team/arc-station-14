using Robust.Shared.GameStates;

namespace Content.Shared._Arc.Evil;

[RegisterComponent, NetworkedComponent]

// Components with this tag cannot be spawned by administrators.
// You will need to find some workaround to spawn these.

// ANY ITEM WITH THIS COMP IS INSPAWNABLE FOR LORE REASONS!! DONT TRY!! FFS!!!
public sealed partial class InspawnableItemComponent : Component
{
    [DataField]
    public string DissapearanceText = "The item fizzles out of existence. Stop trying.";
}
