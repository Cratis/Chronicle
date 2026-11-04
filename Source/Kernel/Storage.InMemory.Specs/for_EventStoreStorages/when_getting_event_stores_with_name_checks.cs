// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Naming;
using Cratis.Chronicle.Storage.Naming.for_ResourceNameReservations;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.InMemory.for_EventStoreStorages;

public class when_getting_event_stores_with_name_checks : Specification
{
    EventStoreStorages _storages;
    ResourceNameSnapshot _snapshot;
    Exception _hostile;
    Exception _caseVariant;
    Exception _quarantined;

    void Establish()
    {
        _snapshot = ResourceNameSnapshot.NotReady();
        _snapshot.Publish([], []);
        var gate = new ResourceNameGate(_snapshot, new FakeReservations(), ResourceNameExemptions.None, NullLogger<ResourceNameGate>.Instance);
        _storages = new(new KnownInstancesOf<ISinkFactory>([]), Substitute.For<Cratis.Orleans.Storage.IJobsStorage>(), gate);
        _storages.GetOrCreate("orders");
    }

    void Because()
    {
        _hostile = Catch.Exception(() => _storages.GetOrCreate("a#b"));
        _caseVariant = Catch.Exception(() => _storages.GetOrCreate("ORDERS"));
        _snapshot.Publish([new EventStoreName("orders")], []);
        _quarantined = Catch.Exception(() => _storages.GetOrCreate("orders"));
    }

    [Fact] void should_refuse_a_name_with_a_reserved_character() => _hostile.ShouldBeOfExactType<InvalidResourceName>();
    [Fact] void should_refuse_a_case_variant_of_a_registered_name() => _caseVariant.ShouldBeOfExactType<ResourceNameConflict>();
    [Fact] void should_refuse_a_quarantined_name() => _quarantined.ShouldBeOfExactType<ResourceQuarantined>();

    void Destroy() => _storages.Dispose();
}
