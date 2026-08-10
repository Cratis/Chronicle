// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Seeding;

namespace Cratis.Chronicle.Namespaces.for_NamespacesReactor.when_a_namespace_is_added;

/// <summary>
/// A reactor observation must not acknowledge a namespace as bootstrapped while one of its global seed facts was
/// rejected. Failing the observation lets Chronicle retry the idempotent seeding operation.
/// </summary>
public class and_namespace_seeding_is_incomplete : given.a_namespaces_reactor
{
    Exception _error;

    void Establish()
    {
        GlobalSeeds(AnEntry());
        _namespaceGrain.SeedWithResult(Arg.Any<IEnumerable<SeedingEntry>>()).Returns(Task.FromResult(SeedingResult.Incomplete));
    }

    async Task Because() => _error = await Catch.Exception(() => AddNamespace());

    [Fact] void should_fail_the_observation() => _error.ShouldBeOfExactType<EventSeedingIncomplete>();
    [Fact] void should_offer_the_entries_once() => _namespaceGrain.Received(1).SeedWithResult(Arg.Any<IEnumerable<SeedingEntry>>());
}
