// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.Mutations.for_EventSequenceMutationRegistry.given;

public class a_mutation_registry : Specification
{
    private protected EventSequenceMutationRegistryState _state;
    protected IEventSequenceMutationRegistry _registry;
    protected EventSequenceMutationIdentity _target;
    protected EventSequenceMutationTarget _proposedTarget;
    protected EventSequenceMutationRequest _request;

    void Establish()
    {
        _state = new();
        _registry = Registry(_state);
        _target = Identity("target-sequence");
        _proposedTarget = new(10UL, 13UL, 3UL);
        _request = Request(_target);
    }

    private protected static IEventSequenceMutationRegistry Registry(EventSequenceMutationRegistryState? state = null) =>
        new EventSequenceMutationRegistry("event-store", "namespace", state ?? new());

    protected static EventSequenceMutationIdentity Identity(string display) =>
        EventSequenceMutationIdentity.TryCreate(display).Identity!;

    protected static EventSequenceMutationRequest Request(
        EventSequenceMutationIdentity target,
        ulong originSequenceNumber = 42,
        string payload = "{\"name\":\"Ada\"}",
        string hash = "command-hash")
    {
        var origin = Identity("origin-sequence");
        const EventSequenceMutationKind kind = EventSequenceMutationKind.Revision;
        var id = EventSequenceMutationDigestCalculator.CalculateId(target, origin, originSequenceNumber, kind);
        return new(
            id,
            target,
            new(origin, originSequenceNumber),
            kind,
            new(payload, hash));
    }

    protected static async Task<EventSequenceMutationRegistryTransitionResult> Apply(
        IEventSequenceMutationRegistry registry,
        EventSequenceMutationBeginResult begin,
        EventSequenceMutationTransition transition) =>
        await registry.Transition(begin.Active!.TargetSequence, begin.Token!, transition);
}
