using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Content.Server.Administration;
using Content.Server.Inventory;
using Content.Server.Popups;
using Content.Server.Preferences.Managers;
using Content.Shared._Arc.Evil;
using Content.Shared.Clothing.Components;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Content.Server.Actions;
using Content.Server.Humanoid;
using Content.Shared.Buckle;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Polymorph;
using Robust.Server.Audio;
using Robust.Server.Containers;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Server.Station.Systems;
using Content.Server.Mind.Commands;
using Content.Shared.Inventory;
using static Content.Shared.Inventory.InventorySystem;
using Content.Shared.Hands.Components;
using Content.Server.Chat.Managers;
using Robust.Shared.Player;

namespace Content.Server._Arcadis.Evil;

public sealed class CursedVisorSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _playerMan = default!;
    [Dependency] private readonly IComponentFactory _compFact = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly StationSpawningSystem _spawning = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly ServerInventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPlayerLocator _playerLocator = default!;
    [Dependency] private readonly EntityManager _entMan = default!;
    [Dependency] private readonly MapSystem _mapManager = default!;
    [Dependency] private readonly IServerPreferencesManager _prefs = default!;
    public EntityUid? PausedMap { get; private set; }
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VisorConvertedComponent, VisorRemoveActionEvent>(OnVisorRemoveEvent);
        SubscribeLocalEvent<CursedVisorComponent, EntGotInsertedIntoContainerMessage>(OnClothingEquipped);
        SubscribeLocalEvent<CursedVisorComponent, EntRemovedFromContainerMessage>(OnClothingUnequipped);
    }

    private void OnClothingUnequipped(EntityUid uid, CursedVisorComponent component,
        EntRemovedFromContainerMessage args)
    {
        var target = args.Container.Owner;

        if (args.Container.ID != component.EquipTo)
            return;

        if (!TryComp<VisorConvertedComponent>(target, out var convertedComp))
            return;

        OnVisorRemoveEvent(uid, convertedComp, null);
    }
    private void OnVisorRemoveEvent(EntityUid uid, VisorConvertedComponent component,
        VisorRemoveActionEvent? _)
    {
        if (Deleted(uid))
            return;

        var parent = component.Parent;
        if (Deleted(parent))
            return;

        var uidXform = Transform(uid);
        var parentXform = Transform(parent);

        if (!TryComp(component.VisorEnt, out CursedVisorComponent? visorComp))
            return;

        if (!TryComp<MetaDataComponent>(uid, out var childMetadata) || !TryComp<MetaDataComponent>(parent, out var targetMetadata))
            return;

        _transform.SetParent(parent, parentXform, uidXform.ParentUid);
        _transform.SetCoordinates(parent, parentXform, uidXform.Coordinates, uidXform.LocalRotation);

        if (visorComp.PopupOnDequip != null)
            _popup.PopupCoordinates(Loc.GetString(visorComp.PopupOnDequip, ("newName", childMetadata.EntityName), ("oldName", targetMetadata.EntityName)), uidXform.Coordinates, PopupType.Medium);

        // move the visor to inhand (or drop if no hands)
        if (HasComp<HandsComponent>(parent) && _hands.TryGetEmptyHand(parent, out var hand))
            _hands.DoPickup(parent, hand, component.VisorEnt);
        else
            _transform.SetCoordinates(component.VisorEnt, Transform(component.VisorEnt), parentXform.Coordinates); // yeet

        _inventory.TransferEntityInventories(uid, parent);

        foreach (var held in _hands.EnumerateHeld(uid))
        {
            _hands.TryDrop(uid, held);
            _hands.TryPickupAnyHand(parent, held, checkActionBlocker: false);
        }

        if (_mindSystem.TryGetMind(uid, out var mindId, out var mind))
            _mindSystem.TransferTo(mindId, parent, mind: mind);

        // if an item polymorph was picked up, put it back down after reverting
        _transform.AttachToGridOrMap(parent, parentXform);
        QueueDel(uid);
    }

    public async void OnClothingEquipped(EntityUid uid, CursedVisorComponent component, EntGotInsertedIntoContainerMessage args)
    {
        var target = args.Container.Owner;

        if (HasComp<VisorConvertedComponent>(target))
            return;

        if (args.Container.ID != component.EquipTo)
            return;

        var playerData = await _playerLocator.LookupIdByNameAsync(component.PlayerCopyFrom);
        if (playerData == null)
            return;

        if (!FetchCharacters(playerData.UserId, out var characters))
            return;

        HumanoidCharacterProfile? character = characters[component.PlayerCharacterCopyFromIndex];

        if (character == null)
            return;

        _buckle.TryUnbuckle(target, target, true);

        var targetTransformComp = Transform(target);

        var child = _spawning.SpawnPlayerMob(targetTransformComp.Coordinates, null, character, null);

        MakeSentientCommand.MakeSentient(child, EntityManager);

        if (component.HideItem)
            TransferEntityInventoriesExceptMask(target, child, component.EquipTo);
        else
            _inventory.TransferEntityInventories(target, child);

        var convertedComp = _compFact.GetComponent<VisorConvertedComponent>();
        convertedComp.Parent = target;
        convertedComp.VisorEnt = uid;
        AddComp(child, convertedComp);

        var childXform = Transform(child);
        _transform.SetLocalRotation(child, targetTransformComp.LocalRotation, childXform);

        if (!TryComp<MetaDataComponent>(child, out var childMetadata) || !TryComp<MetaDataComponent>(target, out var targetMetadata))
            return;

        if (component.ChatMesssageOnEquip != null)
        {
            if (TryComp<ActorComponent>(target, out var actorComp))
            {
                var session = actorComp.PlayerSession;
                if (session == null)
                    return;
                var chatMessage = Loc.GetString(component.ChatMesssageOnEquip, ("name", childMetadata.EntityName));
                var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", chatMessage));
                _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server,
                chatMessage,
                wrappedMessage,
                default,
                false,
                session.Channel,
                Color.Gray);
                return;
            }
        }

        if (component.PopupOnEquip != null)
            _popup.PopupCoordinates(Loc.GetString(component.PopupOnEquip, ("newName", childMetadata.EntityName), ("oldName", targetMetadata.EntityName)), targetTransformComp.Coordinates, PopupType.MediumCaution);

        if (_container.TryGetContainingContainer((target, targetTransformComp, null), out var cont))
            _container.Insert(child, cont);

        foreach (var hand in _hands.EnumerateHeld(target))
        {
            _hands.TryDrop(target, hand, checkActionBlocker: false);
            _hands.TryPickupAnyHand(child, hand);
        }

        if (_mindSystem.TryGetMind(target, out var mindId, out var mind))
            _mindSystem.TransferTo(mindId, child, mind: mind);

        _actions.AddAction(child, ref convertedComp.Action, out var action, component.RevertActionPrototype);

        EnsurePausedMap();
        if (PausedMap != null)
            _transform.SetParent(target, targetTransformComp, PausedMap.Value);
    }

    private void EnsurePausedMap()
    {
        if (PausedMap != null && Exists(PausedMap))
            return;

        var newmap = _mapManager.CreateMap(false);
        _mapManager.SetPaused(newmap, true);
        PausedMap = newmap;
    }

    private bool FetchCharacters(NetUserId player, out HumanoidCharacterProfile[] characters)
    {
        characters = null!;
        PlayerPreferences? prefs = _prefs.GetPreferencesOrNull(player);
        if (prefs == null)
            return false;

        characters = prefs.Characters
            .Where(kv => kv.Value is HumanoidCharacterProfile)
            .Select(kv => (HumanoidCharacterProfile) kv.Value)
            .ToArray();

        return true;
    }

    public void TransferEntityInventoriesExceptMask(Entity<InventoryComponent?> source, Entity<InventoryComponent?> target, string slotToIgnore = "mask")
    {
        if (!Resolve(source.Owner, ref source.Comp) || !Resolve(target.Owner, ref target.Comp))
            return;

        var enumerator = new InventorySlotEnumerator(source.Comp);
        while (enumerator.NextItem(out var item, out var slot))
        {
            if (slot.Name == slotToIgnore)
                continue;

            if (_inventory.TryUnequip(source, slot.Name, true, true, inventory: source.Comp))
                _inventory.TryEquip(target, item, slot.Name, true, true, inventory: target.Comp);
        }
    }
}
