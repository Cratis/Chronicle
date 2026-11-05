// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Generations;

namespace Cratis.Chronicle.Observation.for_GenerationInitializationReplay.when_starting;

public class and_the_store_was_never_initialized : Specification
{
    IStorage _storage;
    IGenerationInitializationStorage _audit;
    readonly ObserverKey _key = new("projection", new("orders"), EventStoreNamespaceName.Default, EventSequenceId.Log);

    void Establish()
    {
        _audit = Substitute.For<IGenerationInitializationStorage>();
        _audit.GetReceipts().Returns(Task.FromResult<IReadOnlyList<GenerationInitializationReceipt>>([]));
        var store = Substitute.For<IEventStoreStorage>();
        store.GenerationInitialization.Returns(_audit);
        _storage = Substitute.For<IStorage>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(store);
    }

    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => GenerationInitializationReplay.EnsureStartAllowed(_storage, _key, ObserverType.Projection));

    [Fact] void should_allow_the_replay() => _error.ShouldBeNull();
    [Fact] async Task should_not_write_receipts() => await _audit.DidNotReceive().TryRecord(Arg.Any<GenerationInitializationReceipt>());
    [Fact] async Task should_not_record_blockers() => await _audit.DidNotReceive().RecordBlocker(Arg.Any<GenerationInitializationBlocker>());
    [Fact] async Task should_not_resolve_blockers() => await _audit.DidNotReceive().ResolveBlocker(Arg.Any<GenerationInitializationBlockerKind>(), Arg.Any<string>(), Arg.Any<DateTimeOffset>());
}
