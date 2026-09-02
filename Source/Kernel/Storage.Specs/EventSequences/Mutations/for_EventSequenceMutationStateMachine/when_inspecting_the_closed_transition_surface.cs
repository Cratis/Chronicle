// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationStateMachine;

public class when_inspecting_the_closed_transition_surface : Specification
{
    string[] _transitions;

    void Because() => _transitions = Enum.GetNames<EventSequenceMutationTransition>();

    [Fact]
    void should_only_represent_the_sentinel_and_active_state_transitions() =>
        _transitions.ShouldContainOnly(
            [
                nameof(EventSequenceMutationTransition.Unspecified),
                nameof(EventSequenceMutationTransition.BeginApplying),
                nameof(EventSequenceMutationTransition.BeginVerifying),
                nameof(EventSequenceMutationTransition.Block),
                nameof(EventSequenceMutationTransition.Resume),
                nameof(EventSequenceMutationTransition.CommitSourceWithoutRepair),
                nameof(EventSequenceMutationTransition.CommitSourceWithRepair),
                nameof(EventSequenceMutationTransition.BeginRepairDispatch),
                nameof(EventSequenceMutationTransition.AcceptRepair),
                nameof(EventSequenceMutationTransition.MarkRepairUnknown)
            ]);
}
