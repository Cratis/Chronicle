// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_DigestKeyedEventSequenceMutationRegistry;

public class when_the_key_is_unavailable : Specification
{
    IEventSequenceMutationRegistry _inner;
    DigestKeyedEventSequenceMutationRegistry _registry;
    EventSequenceMutationBeginResult _begin;
    EventSequenceMutationRegistryTransitionResult _transition;
    EventSequenceMutationRegistryArchiveResult _archive;
    EventSequenceMutationTrackingResult _tracking;

    void Establish()
    {
        _inner = Substitute.For<IEventSequenceMutationRegistry>();
        _registry = new("event-store", UnavailableEventSequenceMutationDigestKeys.Instance, _inner);
    }

    async Task Because()
    {
        _begin = await _registry.Begin(null!, null!);
        _transition = await _registry.Transition(null!, null!, default);
        _archive = await _registry.Archive(null!, null!);
        _tracking = await _registry.BeginTracking(null!, default);
    }

    [Fact] void should_fail_begin_closed() => _begin.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Corrupt);
    [Fact] void should_fail_transition_closed() => _transition.Outcome.ShouldEqual(EventSequenceMutationRegistryTransitionOutcome.Corrupt);
    [Fact] void should_fail_archive_closed() => _archive.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.Corrupt);
    [Fact] void should_fail_tracking_closed() => _tracking.Outcome.ShouldEqual(EventSequenceMutationTrackingOutcome.Corrupt);
    [Fact] void should_never_reach_the_provider() => _inner.ReceivedCalls().ShouldBeEmpty();
}
