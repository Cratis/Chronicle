// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_owners_declare_different_counts_of_a_shared_seed : given.overlapping_global_seed_groups
{
    Task Establish() => _grain.SeedGlobalWithResult([AnEntry(1), AnEntry(1)], true, "first");

    async Task Because()
    {
        await _grain.SeedGlobalWithResult([AnEntry(1)], true, "second");
        await ApplyToFutureNamespace();
    }

    [Fact] void should_use_the_largest_count_for_future_namespaces() => _futureCount.ShouldEqual(2);
    [Fact] void should_not_append_the_smaller_declaration_again() => AppendedCount(_existingEvents).ShouldEqual(2);
}
