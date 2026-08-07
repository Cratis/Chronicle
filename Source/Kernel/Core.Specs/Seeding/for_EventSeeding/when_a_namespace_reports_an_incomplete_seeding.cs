// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Seeding;
using Cratis.Chronicle.Namespaces;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

/// <summary>
/// Seeding is global by default, and the global grain is what decides whether a namespace ever sees an
/// entry again. A namespace grain that quietly declined to append part of what it was handed used to be
/// indistinguishable from one that appended all of it, so the global tracking was committed either way and
/// the entries were never re-dispatched. Leaving a rejected batch unrecorded in the namespace grain buys
/// nothing unless the global grain also holds back - re-dispatch is what a retry is made of, and every
/// namespace grain guards its own idempotency, so re-dispatching what did land costs nothing.
/// </summary>
public class when_a_namespace_reports_an_incomplete_seeding : given.a_global_event_seeding_grain
{
    IEnumerable<SeedingEntry> _entries;
    INamespaces _namespaces;
    IEventSeeding _firstNamespaceGrain;
    IEventSeeding _secondNamespaceGrain;

    void Establish()
    {
        _entries = [AnEntry(1), AnEntry(2)];

        _namespaces = Substitute.For<INamespaces>();
        _namespaces.GetAll().Returns(Task.FromResult<IEnumerable<EventStoreNamespaceName>>(["namespace-1", "namespace-2"]));
        _grainFactory.GetGrain<INamespaces>(Arg.Any<string>()).Returns(_namespaces);

        _firstNamespaceGrain = Substitute.For<IEventSeeding>();
        _secondNamespaceGrain = Substitute.For<IEventSeeding>();
        _firstNamespaceGrain.Seed(Arg.Any<IEnumerable<SeedingEntry>>()).Returns(Task.FromResult(SeedingResult.Incomplete));
        _secondNamespaceGrain.Seed(Arg.Any<IEnumerable<SeedingEntry>>()).Returns(Task.FromResult(SeedingResult.Complete));

        _grainFactory.GetGrain<IEventSeeding>(EventSeedingKey.ForNamespace("TestEventStore", "namespace-1").ToString()).Returns(_firstNamespaceGrain);
        _grainFactory.GetGrain<IEventSeeding>(EventSeedingKey.ForNamespace("TestEventStore", "namespace-2").ToString()).Returns(_secondNamespaceGrain);
    }

    async Task Because()
    {
        await _grain.Seed(_entries);
        await _grain.Seed(_entries);
    }

    [Fact] void should_still_dispatch_to_every_namespace() => _secondNamespaceGrain.Received(2).Seed(Arg.Any<IEnumerable<SeedingEntry>>());
    [Fact] void should_dispatch_to_the_namespace_that_declined_again() => _firstNamespaceGrain.Received(2).Seed(Arg.Any<IEnumerable<SeedingEntry>>());
    [Fact] void should_not_record_the_entries_as_globally_seeded() => TrackedByEventType.ShouldBeEmpty();
    [Fact] void should_not_persist_a_claim_no_namespace_can_back() => _state.DidNotReceive().WriteStateAsync();
}
