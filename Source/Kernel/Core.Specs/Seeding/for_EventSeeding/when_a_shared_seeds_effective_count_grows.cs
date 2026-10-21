// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_a_shared_seeds_effective_count_grows : given.overlapping_global_seed_groups
{
    Task Establish() => _grain.SeedGlobalWithResult([AnEntry(1)], true, "first");

    async Task Because()
    {
        await _grain.SeedGlobalWithResult([AnEntry(1), AnEntry(1)], true, "second");
        await ApplyToFutureNamespace();
    }

    [Fact] void should_append_only_the_missing_occurrence_to_existing_namespaces() => AppendedCount(_existingEvents).ShouldEqual(2);
    [Fact] void should_seed_future_namespaces_with_the_full_new_count() => _futureCount.ShouldEqual(2);
}
