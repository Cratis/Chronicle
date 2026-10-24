// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Storage.InMemory.Generations.for_GenerationAdmission;

public class when_resetting_an_initialized_store : given.a_provider
{
    Exception _error;

    async Task Establish()
    {
        await Advance(1);
        await _storage.GetEventStore(_name).GenerationInitialization.TryRecord(new(_name, GenerationInitializationStep.FloorVerified, "store", "evidence", new(Guid.NewGuid()), 1, DateTimeOffset.UtcNow));
        _storage.GetEventStore(new("uninitialized"));
    }

    void Because() => _error = Catch.Exception(_storage.Clear);

    [Fact] void should_refuse_reset() => _error.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] async Task should_leave_every_store_registered() => (await _storage.GetEventStores()).Count().ShouldEqual(2);
    [Fact] async Task should_keep_the_receipt() => (await _storage.GetEventStore(_name).GenerationInitialization.GetReceipts()).Count.ShouldEqual(1);
    [Fact] async Task should_keep_the_floor() => (await _storage.ReadGenerationFloors())[_name].Revision.ShouldEqual(1UL);
}
