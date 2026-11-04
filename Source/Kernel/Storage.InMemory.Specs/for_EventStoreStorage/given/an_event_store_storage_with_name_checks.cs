// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Naming;
using Cratis.Chronicle.Storage.Naming.for_ResourceNameReservations;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.InMemory.for_EventStoreStorage.given;

public class an_event_store_storage_with_name_checks : Specification
{
    protected static readonly EventStoreName _eventStore = "SomeEventStore";
    protected ResourceNameSnapshot _snapshot;
    protected EventStoreStorage _storage;

    void Establish()
    {
        _snapshot = ResourceNameSnapshot.NotReady();
        _snapshot.Publish([], []);
        var gate = new ResourceNameGate(_snapshot, new FakeReservations(), ResourceNameExemptions.None, NullLogger<ResourceNameGate>.Instance);
        _storage = new EventStoreStorage(
            _eventStore,
            @namespace => new Chronicle.Storage.Sinks.Sinks(_eventStore, @namespace, new KnownInstancesOf<ISinkFactory>([])),
            Substitute.For<Cratis.Orleans.Storage.IJobsStorage>(),
            new NamespaceOpeningGuard(gate, () => Task.FromResult<IEnumerable<string>>([_eventStore.Value])));
    }
}
