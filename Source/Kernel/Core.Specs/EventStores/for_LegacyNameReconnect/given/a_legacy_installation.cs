// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.InMemory;
using Cratis.Chronicle.Storage.Naming;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventStores.for_LegacyNameReconnect.given;

public class a_legacy_installation : Specification
{
    protected static readonly EventStoreName _legacyStore = "legacy$store";
    protected static readonly EventStoreName _plainStore = "shop";
    protected static readonly EventStoreNamespaceName _legacyNamespace = "legacy$tenant";
    protected ResourceNameSnapshot _snapshot;
    protected ResourceNameGate _gate;
    protected EventStoreStorages _storages;
    protected Storage.IStorage _storage;

    void Establish()
    {
        _snapshot = ResourceNameSnapshot.NotReady();
        _snapshot.Publish([], []);
        _gate = new(_snapshot, new InMemoryResourceNameReservations(), new ResourceNameExemptions([(_legacyStore.Value, [])]), NullLogger<ResourceNameGate>.Instance);
        _storages = new(new KnownInstancesOf<ISinkFactory>([]), Substitute.For<Cratis.Orleans.Storage.IJobsStorage>(), _gate);
        _storage = new Storage.InMemory.Storage(_storages, Substitute.For<Storage.ISystemStorage>());

        // The legacy names are already in use, as the check of the names in use records them when the server starts.
        // The in-memory storage can only be given a legacy event store through an exemption.
        _gate.MarkRegistered(_legacyStore);
        _gate.MarkRegistered(_plainStore);
        _gate.MarkRegistered(_plainStore, _legacyNamespace);
        _storage.GetEventStore(_legacyStore);
        _storage.GetEventStore(_plainStore).Namespaces.Ensure(_legacyNamespace).GetAwaiter().GetResult();
    }

    void Destroy() => _storages.Dispose();
}
