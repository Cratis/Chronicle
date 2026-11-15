// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelStorage;

using Cratis.Chronicle.Events;
using KernelStorage::Cratis.Chronicle.Storage.EventSequences.Mutations;
using KernelMutations = KernelConcepts::Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Testing.EventSequences.for_TestingEventStore;

public class when_beginning_a_mutation : Specification
{
    TestingEventStore _store;
    EventSequenceMutationBeginResult _result;

    void Establish() => _store = new TestingEventStore("event-store", "default", () => Substitute.For<IEventTypes>());

    async Task Because()
    {
        var target = KernelMutations.EventSequenceMutationIdentity.TryCreate("target-sequence").Identity!;
        var origin = KernelMutations.EventSequenceMutationIdentity.TryCreate("origin-sequence").Identity!;
        const EventSequenceMutationKind kind = EventSequenceMutationKind.Revision;
        var request = new EventSequenceMutationRequest(
            EventSequenceMutationDigestCalculator.CalculateId(target, origin, 42UL, kind),
            target,
            new(origin, 42UL),
            kind,
            new("{\"name\":\"Ada\"}", "command-hash"));
        var registry = _store.Storage.GetEventStore("event-store").GetNamespace("default").EventSequenceMutations;
        _result = await registry.Begin(request, new(10UL, 13UL, 3UL));
    }

    [Fact] void should_reserve_the_mutation() => _result.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Reserved);

    void Destroy() => _store.Dispose();
}
