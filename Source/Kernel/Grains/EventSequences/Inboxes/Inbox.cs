// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Inboxes;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Inbox;

/// <summary>
/// Represents an implementation of <see cref="IInbox"/>.
/// </summary>
public class Inbox : Grain, IInbox
{
    /// <inheritdoc/>
    public async Task Start()
    {
        var microserviceId = this.GetPrimaryKey(out var keyAsString);
        var key = InboxKey.Parse(keyAsString);

        var observer = GrainFactory.GetGrain<IObserver>(
            microserviceId,
            new ObserverKey(
                microserviceId,
                key.TenantId,
                EventSequenceId.Outbox,
                key.MicroserviceId,
                key.TenantId));

        await observer.SetNameAndType($"Inbox for ${microserviceId}, Outbox from ${key.MicroserviceId} for Tenant ${key.TenantId}", ObserverType.Inbox);
        await observer.Subscribe<IInboxObserverSubscriber>(Enumerable.Empty<EventType>());
    }
}
