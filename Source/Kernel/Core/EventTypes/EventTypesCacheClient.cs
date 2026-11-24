// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Orleans.Runtime.Services;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents an implementation of <see cref="IEventTypesCacheClient"/> that tells the event type registry of every
/// active silo in the cluster that an event type changed.
/// </summary>
/// <remarks>
/// The silos are read from the local cluster membership snapshot, so announcing a change makes no grain call to find
/// them. The registry of a silo that misses the announcement catches up on its next lookup miss or at the end of the
/// safety-net lifetime of its snapshot.
/// </remarks>
/// <param name="membership">The <see cref="IClusterMembershipService"/> that knows the active silos.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> used to reach the grain service of a silo.</param>
public class EventTypesCacheClient(IClusterMembershipService membership, IServiceProvider serviceProvider)
    : GrainServiceClient<IEventTypesCacheGrainService>(serviceProvider), IEventTypesCacheClient
{
    /// <inheritdoc/>
    public async Task Invalidate(EventStoreName eventStore, EventTypeId eventTypeId)
    {
        var silos = membership.CurrentSnapshot.Members.Values
            .Where(_ => _.Status == SiloStatus.Active)
            .Select(_ => _.SiloAddress);
        await Task.WhenAll(silos.Select(silo => GetGrainService(silo).Invalidate(eventStore, eventTypeId)));
    }
}
