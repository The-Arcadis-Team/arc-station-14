using Content.Shared.FixedPoint;
using Content.Shared.Roles;
using Content.Shared.Storage;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using System;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Gamerule that does shit
/// </summary>
[RegisterComponent, Access(typeof(FleshRuleSystem))]
public sealed partial class FleshRuleComponent : Component
{

    /// <summary>
    /// The gear all players spawn with.
    /// </summary>
    [DataField("gear", customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>)), ViewVariables(VVAccess.ReadWrite)]
    public string Gear = "survivalGear";


    /// <summary>
    /// Time to emergency shuttle to arrive if RoundEndBehavior is ShuttleCall.
    /// </summary>
    [DataField]
        public TimeSpan EvacShuttleTime = TimeSpan.FromMinutes(3);

}
