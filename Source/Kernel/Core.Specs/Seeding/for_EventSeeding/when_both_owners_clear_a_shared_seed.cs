// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_both_owners_clear_a_shared_seed : given.overlapping_global_seed_groups
{
    async Task Establish()
    {
        await _grain.SeedGlobalWithResult([AnEntry(1)], true, "first");
        await _grain.SeedGlobalWithResult([AnEntry(1)], true, "second");
        await _grain.SeedGlobalWithResult([], true, "first");
    }

    async Task Because()
    {
        await _grain.SeedGlobalWithResult([], true, "second");
        await ApplyToFutureNamespace();
    }

    [Fact] void should_leave_no_seed_for_future_namespaces() => _futureCount.ShouldEqual(0);
    [Fact] void should_not_retract_existing_events() => AppendedCount(_existingEvents).ShouldEqual(1);
}
