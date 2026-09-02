// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.Mutations.for_EventSequenceMutationRegistry;

public class when_beginning_tracking : given.a_mutation_registry
{
    EventSequenceMutationTrackingResult _began;
    EventSequenceMutationTrackingResult _alreadyTracking;
    EventSequenceMutationTrackingResult _sealedConflict;
    EventSequenceMutationBeginResult _resumed;
    EventSequenceMutation _activeBeforeTracking;
    EventSequenceMutationStateToken _tokenBeforeTracking;
    bool _hasSeal;

    async Task Because()
    {
        var begin = await _registry.Begin(_request, _proposedTarget);
        _activeBeforeTracking = begin.Active!;
        _tokenBeforeTracking = begin.Token!;
        _began = await _registry.BeginTracking(_target, EventSequenceMutationCoverage.Untracked);
        _alreadyTracking = await _registry.BeginTracking(_target, EventSequenceMutationCoverage.Untracked);
        _resumed = await _registry.Begin(_request, _proposedTarget);
        _hasSeal = typeof(IEventSequenceMutationRegistry).GetMethods().Any(_ => _.Name.Contains("Seal", StringComparison.Ordinal));

        var sealedState = new EventSequenceMutationRegistryState();
        var heads = (IDictionary)typeof(EventSequenceMutationRegistryState)
            .GetField("_heads", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(sealedState)!;
        heads[_target.Key] = EventSequenceMutationHead.Initial with { Coverage = EventSequenceMutationCoverage.Sealed };
        _sealedConflict = await Registry(sealedState).BeginTracking(_target, EventSequenceMutationCoverage.Untracked);
    }

    [Fact] void should_begin_tracking() => _began.Outcome.ShouldEqual(EventSequenceMutationTrackingOutcome.Began);
    [Fact] void should_report_tracking_as_idempotent() => _alreadyTracking.Outcome.ShouldEqual(EventSequenceMutationTrackingOutcome.AlreadyTracking);
    [Fact] void should_preserve_the_complete_active_mutation() => _resumed.Active.ShouldEqual(_activeBeforeTracking);
    [Fact] void should_preserve_the_complete_fencing_token() => _resumed.Token.ShouldEqual(_tokenBeforeTracking);
    [Fact] void should_report_sealed_coverage_as_a_conflict() => _sealedConflict.Coverage.ShouldEqual((EventSequenceMutationCoverage?)EventSequenceMutationCoverage.Sealed);
    [Fact] void should_not_expose_a_seal_operation() => _hasSeal.ShouldBeFalse();
}
