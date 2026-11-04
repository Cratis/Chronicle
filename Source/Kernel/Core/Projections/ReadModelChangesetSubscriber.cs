// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Observation.Placement;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IReadModelChangesetSubscriber"/>.
/// </summary>
/// <remarks>
/// Placed on the silo that owns the watch connection via <c language="csharp">[ConnectedObserverPlacement]</c> (the silo
/// address is carried in the <see cref="ObserverSubscriberKey"/>), so the hop to the
/// <see cref="IProjectionChangesetMediator"/> — and from there to the client's gRPC stream — is entirely
/// in-process. Only changesets of the watch's own namespace are forwarded. The subscription identifier is carried in the key's event source id slot.
/// </remarks>
/// <param name="mediator">The <see cref="IProjectionChangesetMediator"/> that owns this silo's watch streams.</param>
[ConnectedObserverPlacement]
public class ReadModelChangesetSubscriber(IProjectionChangesetMediator mediator) : Grain, IReadModelChangesetSubscriber
{
    Guid _subscriptionId;
    EventStoreNamespaceName _namespace = EventStoreNamespaceName.NotSet;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var key = ObserverSubscriberKey.Parse(this.GetPrimaryKeyString());
        _subscriptionId = Guid.Parse(key.EventSourceId.Value);
        _namespace = key.Namespace;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task OnChangeset(EventStoreNamespaceName namespaceName, ReadModelKey readModelKey, JsonObject readModel, ReadModelChangeContext change) =>
        namespaceName == _namespace
            ? mediator.OnChangeset(_subscriptionId, namespaceName, readModelKey, readModel, change)
            : Task.CompletedTask;
}
