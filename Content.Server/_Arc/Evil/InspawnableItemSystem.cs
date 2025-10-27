using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Content.Server.Popups;
using Content.Shared._Arc.Evil;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Arcadis.Evil;

public sealed class InspawnableItemSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly EntityManager _entMan = default!;

    [Dependency] private readonly PopupSystem _popup = default!;

    [Dependency] private readonly TransformSystem _transform = default!;

    private string _checkHash = "8d00a62c8b1ed5d7819afcee31155ca300c3e4febe58ab674d42193add7f1dda";
    private int[] _confirmedUIDs = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InspawnableItemComponent, ComponentRemove>(NiceTryFederal);
    }

    private void NiceTryFederal(EntityUid uid, InspawnableItemComponent component, ComponentRemove args)
    {
        _popup.PopupCoordinates(
            _transform.GetMapCoordinates(uid),
            component.DissapearanceText,
            PopupType.LargeCaution);
        _entMan.DeleteEntity(uid); // You tried.
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<InspawnableItemComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!_confirmedUIDs.Contains(uid.Id))
            {
                _popup.PopupCoordinates(
                    _transform.GetMapCoordinates(uid),
                    component.DissapearanceText,
                    PopupType.LargeCaution);
                _entMan.DeleteEntity(uid);
                continue;
            }
        }
    }

    public EntityUid? SpawnInspawnableItem(string prototype, string password, EntityUid requester)
    {
        var hash = ComputeSha256Hash(password);
        if (hash != _checkHash)
            return null;

        var entUid = _entMan.SpawnEntity(prototype, _transform.GetMapCoordinates(requester));

        _confirmedUIDs = _confirmedUIDs.Append(entUid.Id).ToArray();
        return entUid;
    }

    // yes I asked chatgpt for this
    // im not writing this myself
    private static string ComputeSha256Hash(string rawData)
    {
        // Create a SHA256 instance
        using (SHA256 sha256 = SHA256.Create())
        {
            // Convert the input string to bytes and compute the hash
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));

            // Convert the byte array to a hex string
            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes)
                builder.Append(b.ToString("x2"));

            return builder.ToString();
        }
    }
}
