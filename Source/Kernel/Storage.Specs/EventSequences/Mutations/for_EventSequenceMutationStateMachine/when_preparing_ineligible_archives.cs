// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationStateMachine;

public class when_preparing_ineligible_archives : given.a_valid_mutation_state
{
    EventSequenceMutationArchiveResult[] _results;

    void Because()
    {
        var ineligible = ValidStates()
            .Where(mutation => mutation.RepairState is not (EventSequenceMutationRepairState.NotRequired or EventSequenceMutationRepairState.Accepted or EventSequenceMutationRepairState.Unknown))
            .ToArray();
        _results = ineligible
            .Select(mutation => EventSequenceMutationStateMachine.PrepareArchive(_scope, mutation, Token(mutation)))
            .ToArray();
    }

    [Fact] void should_report_conflict_for_every_ineligible_state() => _results.All(_ => _.Outcome == EventSequenceMutationArchiveOutcome.Conflict && !_.IsSuccess).ShouldBeTrue();
    [Fact] void should_not_prepare_any_history() => _results.All(_ => _.History is null).ShouldBeTrue();
}
