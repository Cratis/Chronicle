// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.EventTypes;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.for_EventStoreStorages;

public class when_using_an_event_types_factory : Specification
{
    EventStoreStorages _storages;
    IEventTypesStorage _types;
    EventStoreName _requested;
    IEventStoreStorage _first;
    IEventStoreStorage _second;
    int _calls;

    void Establish()
    {
        _types = Substitute.For<IEventTypesStorage>();
        _storages = new(new KnownInstancesOf<ISinkFactory>([]), Substitute.For<Cratis.Orleans.Storage.IJobsStorage>(), name =>
        {
            _requested = name;
            _calls++;
            return _types;
        });
    }

    void Because()
    {
        _first = _storages.GetOrCreate("test-store");
        _second = _storages.GetOrCreate("test-store");
    }

    void Destroy() => _storages.Dispose();

    [Fact] void should_pass_the_store_identity_to_the_factory() => _requested.Value.ShouldEqual("test-store");
    [Fact] void should_use_the_custom_event_types_storage() => ReferenceEquals(_first.EventTypes, _types).ShouldBeTrue();
    [Fact] void should_share_the_store_after_creation() => ReferenceEquals(_first, _second).ShouldBeTrue();
    [Fact] void should_create_event_types_once_for_the_store() => _calls.ShouldEqual(1);
}
