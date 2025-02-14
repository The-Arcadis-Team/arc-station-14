using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Arc.OuijiBoard;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class OuijiBoardComponent : Component
{
    // Minimum time between each phrase, to prevent spam
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(10);

    // Time that the next phrase can be sent.
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPhrase;
}
