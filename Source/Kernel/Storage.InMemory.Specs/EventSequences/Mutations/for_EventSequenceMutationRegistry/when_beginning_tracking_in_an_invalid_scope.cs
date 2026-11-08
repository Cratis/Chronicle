// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.Mutations.for_EventSequenceMutationRegistry;

public class when_beginning_tracking_in_an_invalid_scope : given.a_mutation_registry
{
    EventSequenceMutationTrackingResult _invalidStore;
    EventSequenceMutationTrackingResult _invalidNamespace;
    EventSequenceMutationTrackingResult _afterInvalidStore;
    EventSequenceMutationTrackingResult _afterInvalidNamespace;

    async Task Because()
    {
        var storeState = new EventSequenceMutationRegistryState();
        var namespaceState = new EventSequenceMutationRegistryState();
        _invalidStore = await new EventSequenceMutationRegistry(EventStoreName.NotSet, "namespace", storeState)
            .BeginTracking(_target, EventSequenceMutationCoverage.Untracked);
        _invalidNamespace = await new EventSequenceMutationRegistry("event-store", EventStoreNamespaceName.NotSet, namespaceState)
            .BeginTracking(_target, EventSequenceMutationCoverage.Untracked);
        _afterInvalidStore = await Registry(storeState).BeginTracking(_target, EventSequenceMutationCoverage.Untracked);
        _afterInvalidNamespace = await Registry(namespaceState).BeginTracking(_target, EventSequenceMutationCoverage.Untracked);
    }

    [Fact] void should_reject_both_invalid_scopes() => (_invalidStore.Outcome == EventSequenceMutationTrackingOutcome.Invalid && _invalidNamespace.Outcome == EventSequenceMutationTrackingOutcome.Invalid).ShouldBeTrue();
    [Fact] void should_report_the_typed_scope_error() => (_invalidStore.Validation!.Error == EventSequenceMutationValidationError.InvalidScope && _invalidNamespace.Validation!.Error == EventSequenceMutationValidationError.InvalidScope).ShouldBeTrue();
    [Fact] void should_not_write_during_invalid_scope_attempts() => (_afterInvalidStore.Outcome == EventSequenceMutationTrackingOutcome.Began && _afterInvalidNamespace.Outcome == EventSequenceMutationTrackingOutcome.Began).ShouldBeTrue();
}
