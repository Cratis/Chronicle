// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.ReadModels.for_ReadModelSinkProvenance;

public class when_discovering_another_store_fails : given.a_provenance_inventory
{
    Exception _error;
    bool _completed;

    void Establish()
    {
        _storage.GetEventStores().Returns(Task.FromResult<IEnumerable<EventStoreName>>([Store, "unavailable"]));
        _storage.GetEventStore((EventStoreName)"unavailable").Returns(_ => throw new InvalidOperationException("offline"));
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _provenance.EnsureReadable(Store, EventStoreNamespaceName.Default, _model));
        _completed = (await Marker(EventStoreNamespaceName.Default).ReadState()).IsComplete;
    }

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<ReadModelIsNotAvailable>();
    [Fact] void should_not_complete_a_partially_discovered_inventory() => _completed.ShouldBeFalse();
}
