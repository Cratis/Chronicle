// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationStateMachine.given;

public class a_valid_mutation_state : Specification
{
    protected EventSequenceKey _scope;
    protected EventSequenceMutationIdentity _targetIdentity;
    protected EventSequenceMutationRequest _request;
    protected EventSequenceMutationTarget _target;
    protected EventSequenceMutationDefinition _definition;
    protected EventSequenceMutation _active;
    protected EventSequenceMutationStateToken _token;

    void Establish()
    {
        _scope = new("target-sequence", "event-store", "namespace");
        _targetIdentity = EventSequenceMutationIdentity.TryCreate("target-sequence").Identity!;
        var originIdentity = EventSequenceMutationIdentity.TryCreate("origin-sequence").Identity!;
        _request = new(
            Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"),
            _targetIdentity,
            new(originIdentity, 42UL),
            EventSequenceMutationKind.Revision,
            new("{\"name\":\"Ada\"}", "command-hash"));
        _target = new(10UL, 13UL, 3UL);
        _definition = EventSequenceMutationDefinition.Create(_scope, _request, _target);
        _active = Mutation(EventSequenceMutationPhase.Reserved);
        _token = EventSequenceMutationStateToken.Create(_scope, _active);
    }

    protected EventSequenceMutation Mutation(
        EventSequenceMutationPhase phase,
        EventSequenceMutationPhase blockedFrom = EventSequenceMutationPhase.None,
        EventSequenceMutationRepairState repairState = EventSequenceMutationRepairState.Unspecified,
        long stateVersion = 1,
        EventSequenceMutationDefinition? definition = null,
        long ordinal = 7) =>
        new(definition ?? _definition, ordinal, stateVersion, phase, blockedFrom, repairState);

    protected EventSequenceMutationStateToken Token(EventSequenceMutation mutation) =>
        EventSequenceMutationStateToken.Create(_scope, mutation);

    protected static EventSequenceMutationStateToken UncheckedToken(EventSequenceKey scope, EventSequenceMutation mutation) =>
        (EventSequenceMutationStateToken)typeof(EventSequenceMutationStateToken)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(_ => _.GetParameters().Length == 2)
            .Invoke([scope, mutation]);

    protected (EventSequenceMutation Source, EventSequenceMutationTransition Transition, EventSequenceMutation Successor)[] LegalTransitions() =>
    [
        (Mutation(EventSequenceMutationPhase.Reserved), EventSequenceMutationTransition.BeginApplying, Mutation(EventSequenceMutationPhase.Applying, stateVersion: 2)),
        (Mutation(EventSequenceMutationPhase.Applying), EventSequenceMutationTransition.BeginVerifying, Mutation(EventSequenceMutationPhase.Verifying, stateVersion: 2)),
        (Mutation(EventSequenceMutationPhase.Applying), EventSequenceMutationTransition.Block, Mutation(EventSequenceMutationPhase.Blocked, EventSequenceMutationPhase.Applying, stateVersion: 2)),
        (Mutation(EventSequenceMutationPhase.Verifying), EventSequenceMutationTransition.Block, Mutation(EventSequenceMutationPhase.Blocked, EventSequenceMutationPhase.Verifying, stateVersion: 2)),
        (Mutation(EventSequenceMutationPhase.Blocked, EventSequenceMutationPhase.Applying), EventSequenceMutationTransition.Resume, Mutation(EventSequenceMutationPhase.Applying, stateVersion: 2)),
        (Mutation(EventSequenceMutationPhase.Blocked, EventSequenceMutationPhase.Verifying), EventSequenceMutationTransition.Resume, Mutation(EventSequenceMutationPhase.Verifying, stateVersion: 2)),
        (Mutation(EventSequenceMutationPhase.Verifying), EventSequenceMutationTransition.CommitSourceWithoutRepair, Mutation(EventSequenceMutationPhase.SourceCommitted, repairState: EventSequenceMutationRepairState.NotRequired, stateVersion: 2)),
        (Mutation(EventSequenceMutationPhase.Verifying), EventSequenceMutationTransition.CommitSourceWithRepair, Mutation(EventSequenceMutationPhase.SourceCommitted, repairState: EventSequenceMutationRepairState.Pending, stateVersion: 2)),
        (Mutation(EventSequenceMutationPhase.SourceCommitted, repairState: EventSequenceMutationRepairState.Pending), EventSequenceMutationTransition.BeginRepairDispatch, Mutation(EventSequenceMutationPhase.SourceCommitted, repairState: EventSequenceMutationRepairState.Dispatching, stateVersion: 2)),
        (Mutation(EventSequenceMutationPhase.SourceCommitted, repairState: EventSequenceMutationRepairState.Dispatching), EventSequenceMutationTransition.AcceptRepair, Mutation(EventSequenceMutationPhase.SourceCommitted, repairState: EventSequenceMutationRepairState.Accepted, stateVersion: 2)),
        (Mutation(EventSequenceMutationPhase.SourceCommitted, repairState: EventSequenceMutationRepairState.Dispatching), EventSequenceMutationTransition.MarkRepairUnknown, Mutation(EventSequenceMutationPhase.SourceCommitted, repairState: EventSequenceMutationRepairState.Unknown, stateVersion: 2))
    ];

    protected EventSequenceMutation[] ValidStates() =>
    [
        Mutation(EventSequenceMutationPhase.Reserved),
        Mutation(EventSequenceMutationPhase.Applying),
        Mutation(EventSequenceMutationPhase.Verifying),
        Mutation(EventSequenceMutationPhase.Blocked, EventSequenceMutationPhase.Applying),
        Mutation(EventSequenceMutationPhase.Blocked, EventSequenceMutationPhase.Verifying),
        Mutation(EventSequenceMutationPhase.SourceCommitted, repairState: EventSequenceMutationRepairState.NotRequired),
        Mutation(EventSequenceMutationPhase.SourceCommitted, repairState: EventSequenceMutationRepairState.Pending),
        Mutation(EventSequenceMutationPhase.SourceCommitted, repairState: EventSequenceMutationRepairState.Dispatching),
        Mutation(EventSequenceMutationPhase.SourceCommitted, repairState: EventSequenceMutationRepairState.Accepted),
        Mutation(EventSequenceMutationPhase.SourceCommitted, repairState: EventSequenceMutationRepairState.Unknown)
    ];
}
