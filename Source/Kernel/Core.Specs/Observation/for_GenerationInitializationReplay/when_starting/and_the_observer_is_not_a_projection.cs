// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Generations;

namespace Cratis.Chronicle.Observation.for_GenerationInitializationReplay.when_starting;

public class and_the_observer_is_not_a_projection : Specification
{
    IStorage _storage;
    IGenerationInitializationStorage _audit;
    readonly ObserverKey _key = new("projection", new("orders"), EventStoreNamespaceName.Default, EventSequenceId.Log);

    void Establish()
    {
        _audit = Substitute.For<IGenerationInitializationStorage>();
        _audit.GetReceipts().Returns(Task.FromResult<IReadOnlyList<GenerationInitializationReceipt>>([new GenerationInitializationReceipt(new("orders"), GenerationInitializationStep.Enabled, "store", "evidence", new(Guid.NewGuid()), 1, DateTimeOffset.UtcNow)]));
        var store = Substitute.For<IEventStoreStorage>();
        store.GenerationInitialization.Returns(_audit);
        _storage = Substitute.For<IStorage>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(store);
    }

    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => GenerationInitializationReplay.EnsureStartAllowed(_storage, _key, ObserverType.Reactor));

    [Fact] void should_allow_the_replay() => _error.ShouldBeNull();
    [Fact] async Task should_not_read_receipts() => await _audit.DidNotReceive().GetReceipts();
}
