// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Storage.Occurrences;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.Occurrences.for_OccurrenceContextBuilder.when_completing;

public class a_detached_lease : Specification
{
    EventStoreForTesting _store;
    OccurrenceLeaseResult _result;

    void Establish() => _store = new(null, new InMemoryClientArtifactsProvider());

    async Task Because()
    {
        var context = new OccurrenceContextBuilder().ForTrigger(new("detached")).WithIdempotencyId("one").BuildDetached();
        var result = await ((IChronicleServicesAccessor)_store.Connection).Services.Occurrences.CompleteOccurrence(new() { EventStore = _store.Name, Namespace = _store.Namespace, Trigger = context.Trigger, OccurrenceId = context.Id, Epoch = context.Lease.Epoch }).EnsureSuccess();
        _result = result.Status;
    }

    [Fact] void should_lose_the_lease() => _result.ShouldEqual(OccurrenceLeaseResult.OccurrenceLeaseLost);

    void Destroy() => _store.Dispose();
}
