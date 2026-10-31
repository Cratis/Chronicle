// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Seeding;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_a_declared_seed_is_also_added_explicitly : given.overlapping_global_seed_groups
{
    Task Establish() => _grain.SeedGlobalWithResult([AnEntry(1)], true, "program");

    async Task Because()
    {
        await _grain.SeedGlobalWithResult([AnEntry(1)], false, "program");
        await _grain.SeedGlobalWithResult([], true, "program");
        await ApplyToFutureNamespace();
    }

    [Fact] void should_retain_the_addition_for_future_namespaces() => _futureCount.ShouldEqual(1);
    [Fact] void should_record_the_added_provenance() => _stored.ByEventSource.Values.Single().Single().Origin.ShouldEqual(SeedOrigin.Added);
    [Fact] void should_not_duplicate_existing_events() => AppendedCount(_existingEvents).ShouldEqual(1);
}
