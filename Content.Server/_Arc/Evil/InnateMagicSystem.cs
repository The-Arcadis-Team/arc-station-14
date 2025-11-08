using Content.Server.Actions;
using Content.Server.Polymorph.Systems;
using Content.Server.Popups;
using Content.Shared._Arc.Evil;
using Content.Shared.Damage;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Arcadis.Evil;

public sealed class InnateMagicSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly EntityManager _entMan = default!;

    [Dependency] private readonly PopupSystem _popup = default!;

    [Dependency] private readonly TransformSystem _transform = default!;

    [Dependency] private readonly PolymorphSystem _poly = default!;

    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InnateMagicComponent, VoidDropFormEvent>(OnVoidDropForm);
        SubscribeLocalEvent<InnateMagicComponent, VoidAmplifyAttacksEvent>(OnVoidAmplifyAttacks);
        SubscribeLocalEvent<VoidAttacksAmplificationComponent, MeleeHitEvent>(OnHitVoid);
    }

    private void OnVoidDropForm(Entity<InnateMagicComponent> ent, ref VoidDropFormEvent args)
    {
        Spawn("PolymorphVoidDropFormAnimation", Transform(ent).Coordinates);

        _poly.PolymorphEntity(ent, "VoidJaunt");
    }

    private void OnVoidAmplifyAttacks(Entity<InnateMagicComponent> ent, ref VoidAmplifyAttacksEvent args)
    {
        if (!TryComp<MetaDataComponent>(ent, out var meta))
            return;
        if (TryComp<VoidAttacksAmplificationComponent>(ent, out var _))
        {
            _entMan.RemoveComponent<VoidAttacksAmplificationComponent>(ent);
            _popup.PopupEntity(Loc.GetString("void-amplification-end-self"), ent, ent, PopupType.Medium);
            _popup.PopupCoordinates(Loc.GetString("void-amplification-end", ("target", meta.EntityName)), Transform(ent).Coordinates, Filter.PvsExcept(ent.Owner), true, PopupType.MediumCaution);

        }
        else
        {
            AddComp<VoidAttacksAmplificationComponent>(ent.Owner);
            _popup.PopupEntity(Loc.GetString("void-amplification-start-self"), ent, ent, PopupType.Medium);
            _popup.PopupCoordinates(Loc.GetString("void-amplification-start", ("target", meta.EntityName)), Transform(ent).Coordinates, Filter.PvsExcept(ent.Owner), true, PopupType.MediumCaution);
        }
    }

    private void OnHitVoid(EntityUid uid, VoidAttacksAmplificationComponent component, MeleeHitEvent args)
    {
        if (!args.IsHit
            || args.HitEntities.Count <= 0)
            return;

        foreach (var target in args.HitEntities)
        {
            if (TryComp<DamageableComponent>(target, out var damageable))
            {
                if (!TryComp<MetaDataComponent>(target, out var meta))
                    return;
                _popup.PopupEntity(Loc.GetString(component.AttackPopup, ("target", meta.EntityName)), target, uid, PopupType.MediumCaution);
                args.BonusDamage += component.DamageMultiplier * args.BaseDamage;
            }

            var attackerPos = _transform.GetWorldPosition(uid);
            var targetPos = _transform.GetWorldPosition(target);
            var delta = targetPos - attackerPos;
            var normalizedDelta = System.Numerics.Vector2.Normalize(delta);
            var flippedDelta = new System.Numerics.Vector2(-normalizedDelta.X, -normalizedDelta.Y); // hehe
            var flingVector = flippedDelta * component.FlingDistance * -1f;
            _throwing.TryThrow(target, flingVector);
        }
    }

    public void GiveActions(EntityUid entityUid)
    {
        if (!TryComp<InnateMagicComponent>(entityUid, out var innateMagicComp))
            return;

        foreach (var actionProto in innateMagicComp.ActionsToAdd)
        {
            _actions.AddAction(entityUid, actionProto);
        }
    }
}
