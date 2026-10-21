// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_explicit_additions_overlap_a_declaration : given.overlapping_global_seed_groups
{
    Task Establish() => _grain.SeedGlobalWithResult([AnEntry(1)], true, "program");

    async Task Because()
    {
        await _grain.SeedGlobalWithResult([AnEntry(1), AnEntry(1)], false, "program");
        await _grain.SeedGlobalWithResult([AnEntry(1)], false, "other");
        await ApplyToFutureNamespace();
    }

    [Fact] void should_preserve_each_provenance_group() => _stored.ByEventSource.Values.Single().Count().ShouldEqual(4);
    [Fact] void should_use_the_largest_group_count_for_existing_namespaces() => AppendedCount(_existingEvents).ShouldEqual(2);
    [Fact] void should_use_the_largest_group_count_for_future_namespaces() => _futureCount.ShouldEqual(2);
}
