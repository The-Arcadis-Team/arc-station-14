using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Arc.Evil;

[RegisterComponent, NetworkedComponent]

// More commonly known as "I'm lazy"
// Handles some special Arcadis-specific magic casting so I don't have to deal with the boilerplate bullshit of making it normal magic.
// Also this is all per-entity anyways so why would I make it something just anyone can have? :3
public sealed partial class InnateMagicComponent : Component
{
    [DataField]
    public List<string> ActionsToAdd = new() { "No", "No", "No", "No", "No" };
}

public sealed partial class AntivoidFormItemEvent : InstantActionEvent;
public sealed partial class VoidAmplifyAttacksEvent : InstantActionEvent;
public sealed partial class VoidDropFormEvent : InstantActionEvent;
