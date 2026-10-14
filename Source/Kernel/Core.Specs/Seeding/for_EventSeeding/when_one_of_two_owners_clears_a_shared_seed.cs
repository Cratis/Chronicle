// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_one_of_two_owners_clears_a_shared_seed : given.overlapping_global_seed_groups
{
    async Task Establish()
    {
        await _grain.SeedGlobalWithResult([AnEntry(1)], true, "first");
        await _grain.SeedGlobalWithResult([AnEntry(1)], true, "second");
    }

    async Task Because()
    {
        await _grain.SeedGlobalWithResult([], true, "first");
        await ApplyToFutureNamespace();
    }

    [Fact] void should_keep_the_shared_seed_for_future_namespaces() => _futureCount.ShouldEqual(1);
    [Fact] void should_leave_existing_events_untouched() => AppendedCount(_existingEvents).ShouldEqual(1);
}
