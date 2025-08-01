using Content.Shared.Clothing.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Content.Shared.Traits;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing.Components;

/// <summary>
///     This handles entities which can be equipped.
/// </summary>
[NetworkedComponent]
[RegisterComponent]
[Access(typeof(ClothingSystem), typeof(InventorySystem))]
public sealed partial class ClothingComponent : Component
{
    [DataField]
    public Dictionary<string, List<PrototypeLayerData>> ClothingVisuals = new();

    /// <summary>
    /// The name of the layer in the user that this piece of clothing will map to
    /// </summary>
    [DataField]
    public string? MappedLayer;

    [DataField]
    public bool QuickEquip = true;

    [DataField(required: true)]
    [Access(typeof(ClothingSystem), typeof(InventorySystem), Other = AccessPermissions.ReadExecute)]
    public SlotFlags Slots = SlotFlags.NONE;

    /// <summary>
    ///   The actual sprite layer to render this entity's equipped sprite to, overriding the layer determined by the slot.
    /// </summary>
    [DataField]
    [Access(typeof(ClothingSystem))]
    public string? RenderLayer;

    [DataField]
    public SoundSpecifier? EquipSound;

    [DataField]
    public SoundSpecifier? UnequipSound;

    [Access(typeof(ClothingSystem))]
    [DataField]
    public string? EquippedPrefix;

    /// <summary>
    /// Allows the equipped state to be directly overwritten.
    /// useful when prototyping INNERCLOTHING items into OUTERCLOTHING items without duplicating/modifying RSIs etc.
    /// </summary>
    [Access(typeof(ClothingSystem))]
    [DataField]
    public string? EquippedState;

    [DataField]
    public string? Sprite;

    /// <summary>
    /// Name of the inventory slot the clothing is in.
    /// </summary>
    public string? InSlot;

    [DataField]
    public TimeSpan EquipDelay = TimeSpan.Zero;

    [DataField]
    public TimeSpan UnequipDelay = TimeSpan.Zero;

    /// <summary>
    /// Offset for the strip time for an entity with this component.
    /// Only applied when it is being equipped or removed by another player.
    /// </summary>
    [DataField]
    public TimeSpan StripDelay = TimeSpan.Zero;

    /// <summary>
    ///     These functions are called when an entity equips an item with this component.
    /// </summary>
    [DataField(serverOnly: true)]
    public TraitFunction[] OnEquipFunctions { get; private set; } = Array.Empty<TraitFunction>();

    /// <summary>
    ///     These functions are called when an entity un-equips an item with this component.
    /// </summary>
    [DataField(serverOnly: true)]
    public TraitFunction[] OnUnequipFunctions { get; private set; } = Array.Empty<TraitFunction>();
}

[Serializable, NetSerializable]
public sealed class ClothingComponentState : ComponentState
{
    public string? EquippedPrefix;

    public ClothingComponentState(string? equippedPrefix)
    {
        EquippedPrefix = equippedPrefix;
    }
}

public enum ClothingMask : byte
{
    NoMask = 0,
    UniformFull,
    UniformTop
}

[Serializable, NetSerializable]
public sealed partial class ClothingEquipDoAfterEvent : DoAfterEvent
{
    public string Slot;

    public ClothingEquipDoAfterEvent(string slot)
    {
        Slot = slot;
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class ClothingUnequipDoAfterEvent : DoAfterEvent
{
    public string Slot;

    public ClothingUnequipDoAfterEvent(string slot)
    {
        Slot = slot;
    }

    public override DoAfterEvent Clone() => this;
}
