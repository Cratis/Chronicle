// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationStateToken;

public class when_creating_a_token : for_EventSequenceMutationStateMachine.given.a_valid_mutation_state
{
    EventSequenceMutationStateToken _result;

    void Because() => _result = EventSequenceMutationStateToken.Create(_scope, _active);

    [Fact] void should_bind_the_event_sequence_id() => _result.Scope.EventSequenceId.ShouldEqual(_scope.EventSequenceId);
    [Fact] void should_bind_the_event_store() => _result.Scope.EventStore.ShouldEqual(_scope.EventStore);
    [Fact] void should_bind_the_namespace() => _result.Scope.Namespace.ShouldEqual(_scope.Namespace);
    [Fact] void should_bind_the_complete_scope() => _result.Scope.ShouldEqual(_scope);
    [Fact] void should_bind_the_canonical_target_key() => _result.TargetKey.ShouldEqual(_targetIdentity.Key);
    [Fact] void should_bind_the_mutation_id() => _result.Id.ShouldEqual(_active.Id);
    [Fact] void should_bind_the_ordinal() => _result.Ordinal.ShouldEqual(_active.Ordinal);
    [Fact] void should_bind_the_definition_digest() => _result.DefinitionDigestV1.ShouldEqual(_active.Definition.DefinitionDigestV1);
    [Fact] void should_bind_the_phase() => _result.Phase.ShouldEqual(_active.Phase);
    [Fact] void should_bind_the_blocked_source() => _result.BlockedFrom.ShouldEqual(_active.BlockedFrom);
    [Fact] void should_bind_the_repair_state() => _result.RepairState.ShouldEqual(_active.RepairState);
    [Fact] void should_bind_the_state_version() => _result.StateVersion.ShouldEqual(_active.StateVersion);
}
