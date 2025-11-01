using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Arc.Evil;

[RegisterComponent, NetworkedComponent]
public sealed partial class CursedVisorComponent : Component
{
    [DataField]
    public string PlayerCopyFrom = "Eris";
    [DataField]
    public int PlayerCharacterCopyFromIndex = 10;
    [DataField]
    public string EquipTo = "mask";

    [DataField]
    public string RevertActionPrototype = "ActionRemoveVisor";

    [DataField]
    public string? ChatMesssageOnEquip = "visor-chat-equip";
    [DataField]
    public string? PopupOnEquip = "visor-popup-equip";

    [DataField]
    public string? PopupOnDequip = "visor-popup-dequip";

}

[RegisterComponent, NetworkedComponent]
public sealed partial class VisorConvertedComponent : Component
{
    [DataField]
    public EntityUid? Action;

    [DataField(required: true)]
    public EntityUid Parent;

    [DataField]
    public EntityUid VisorEnt;
}
public sealed partial class VisorRemoveActionEvent : InstantActionEvent
{

}
