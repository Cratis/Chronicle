// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations.for_EventSequenceMutationRegistry;

public class when_beginning_tracking : given.a_mutation_registry
{
    EventSequenceMutationTrackingResult _began;
    EventSequenceMutationTrackingResult _alreadyTracking;
    EventSequenceMutationTrackingResult _sealedConflict;
    EventSequenceMutationBeginResult _resumed;
    EventSequenceMutation _activeBeforeTracking;
    EventSequenceMutationStateToken _tokenBeforeTracking;

    async Task Because()
    {
        var begin = await _registry.Begin(_request, _proposedTarget);
        _activeBeforeTracking = begin.Active!;
        _tokenBeforeTracking = begin.Token!;

        _began = await _registry.BeginTracking(_target, EventSequenceMutationCoverage.Untracked);
        _alreadyTracking = await _registry.BeginTracking(_target, EventSequenceMutationCoverage.Untracked);

        // Resuming the exact same request must round-trip the active mutation unchanged -
        // beginning tracking is not itself a mutation state transition.
        _resumed = await _registry.Begin(_request, _proposedTarget);

        var sealedTarget = Identity("sealed-target");
        await using (var context = CreateContext())
        {
            context.EventSequenceMutationHeads.Add(new EventSequenceMutationHeadEntry
            {
                EventSequenceId = (EventSequenceId)sealedTarget.Display,
                Coverage = EventSequenceMutationCoverage.Sealed,
                LastAssignedOrdinal = EventSequenceMutationOrdinal.NotSet
            });
            await context.SaveChangesAsync();
        }

        _sealedConflict = await _registry.BeginTracking(sealedTarget, EventSequenceMutationCoverage.Untracked);
    }

    [Fact] void should_begin_tracking() => _began.Outcome.ShouldEqual(EventSequenceMutationTrackingOutcome.Began);
    [Fact] void should_report_tracking_as_idempotent() => _alreadyTracking.Outcome.ShouldEqual(EventSequenceMutationTrackingOutcome.AlreadyTracking);
    [Fact] void should_preserve_the_complete_active_mutation() => _resumed.Active.ShouldEqual(_activeBeforeTracking);
    [Fact] void should_preserve_the_complete_fencing_token() => _resumed.Token.ShouldEqual(_tokenBeforeTracking);
    [Fact] void should_report_sealed_coverage_as_a_conflict() => _sealedConflict.Coverage.ShouldEqual((EventSequenceMutationCoverage?)EventSequenceMutationCoverage.Sealed);
}
