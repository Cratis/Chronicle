// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Folds federation events, in sequence order, into the effective bindings and grants. The last event per event source id wins.
/// </summary>
internal sealed class FederationHistory
{
    readonly Dictionary<string, FederationBindingState> _bindings = new(StringComparer.Ordinal);
    readonly Dictionary<string, FederationGrantState> _grants = new(StringComparer.Ordinal);
    readonly HashSet<string> _bindingIdsEverSeen = new(StringComparer.Ordinal);
    readonly HashSet<string> _grantIdsEverSeen = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the effective bindings.
    /// </summary>
    public IReadOnlyList<FederationBindingState> Bindings => [.. _bindings.Values.OrderBy(_ => _.Id, StringComparer.Ordinal)];

    /// <summary>
    /// Gets the effective grants, revoked ones included.
    /// </summary>
    public IReadOnlyList<FederationGrantState> Grants => [.. _grants.Values.OrderBy(_ => _.Id, StringComparer.Ordinal)];

    /// <summary>
    /// Check whether any event, including removal, has ever been recorded for a binding id.
    /// </summary>
    /// <param name="id">The binding id.</param>
    /// <returns>True when there is history for the id.</returns>
    public bool HasBindingHistory(string id) => _bindingIdsEverSeen.Contains(id);

    /// <summary>
    /// Check whether any event, including revocation, has ever been recorded for a link id.
    /// </summary>
    /// <param name="id">The link id.</param>
    /// <returns>True when there is history for the id.</returns>
    public bool HasGrantHistory(string id) => _grantIdsEverSeen.Contains(id);

    /// <summary>
    /// Check whether a binding currently exists.
    /// </summary>
    /// <param name="id">The binding id.</param>
    /// <returns>True when the binding exists.</returns>
    public bool HasBinding(string id) => _bindings.ContainsKey(id);

    /// <summary>
    /// Check whether a grant currently exists, revoked or not.
    /// </summary>
    /// <param name="id">The link id.</param>
    /// <returns>True when the grant exists.</returns>
    public bool HasGrant(string id) => _grants.ContainsKey(id);

    /// <summary>
    /// Apply an event to the folded state. Events that are not federation events are ignored.
    /// </summary>
    /// <param name="id">The event source id, which is the binding or link id.</param>
    /// <param name="content">The deserialized event.</param>
    public void Apply(string id, object content)
    {
        switch (content)
        {
            case FederationPeerBound bound:
                _bindingIdsEverSeen.Add(id);
                _bindings[id] = new(
                    id,
                    bound.LogicalSource,
                    bound.Endpoint,
                    bound.ServerPin,
                    bound.ClientCertificate,
                    bound.RemoteEventStore,
                    bound.RemoteNamespace,
                    bound.TargetEventStore,
                    bound.TargetNamespace,
                    bound.Origin);
                break;

            case FederationPeerUnbound:
                _bindingIdsEverSeen.Add(id);
                _bindings.Remove(id);
                break;

            case FederationGrantSet set:
                _grantIdsEverSeen.Add(id);
                _grants[id] = new(
                    id,
                    set.ClientPin,
                    set.EventStore,
                    [.. set.Namespaces],
                    set.AllPublic,
                    [.. set.EventTypes],
                    false,
                    set.Origin);
                break;

            case FederationLinkRevoked:
                _grantIdsEverSeen.Add(id);
                if (_grants.TryGetValue(id, out var grant))
                {
                    _grants[id] = grant with { Revoked = true };
                }

                break;
        }
    }
}
