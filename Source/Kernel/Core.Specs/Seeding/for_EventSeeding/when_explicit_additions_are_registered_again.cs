// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_explicit_additions_are_registered_again : given.overlapping_global_seed_groups
{
    Task Establish() => _grain.SeedGlobalWithResult([AnEntry(1), AnEntry(1)], false, "program");

    async Task Because()
    {
        await _grain.SeedGlobalWithResult([AnEntry(1), AnEntry(1)], false, "program");
        await ApplyToFutureNamespace();
    }

    [Fact] void should_not_accumulate_duplicate_additions() => _stored.ByEventSource.Values.Single().Count().ShouldEqual(2);
    [Fact] void should_seed_existing_namespaces_with_the_offered_count_once() => AppendedCount(_existingEvents).ShouldEqual(2);
    [Fact] void should_seed_future_namespaces_with_the_offered_count() => _futureCount.ShouldEqual(2);
}
