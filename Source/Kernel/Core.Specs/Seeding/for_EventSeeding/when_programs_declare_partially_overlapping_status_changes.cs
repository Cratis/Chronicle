// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_programs_declare_partially_overlapping_status_changes : given.overlapping_global_seed_groups
{
    Task Establish() => _grain.SeedGlobalWithResult([StatusChanged("Open"), StatusChanged("Closed")], true, "first");

    async Task Because()
    {
        await _grain.SeedGlobalWithResult([StatusChanged("Closed"), StatusChanged("Pending")], true, "second");
        await ApplyToFutureNamespace();
    }

    [Fact] void should_append_only_the_missing_status_in_existing_namespaces() => AppendedStatuses(_existingEvents).ShouldEqual(["Open", "Closed", "Pending"]);
    [Fact] void should_preserve_first_occurrence_order_in_future_namespaces() => AppendedStatuses(_futureEvents).ShouldEqual(["Open", "Closed", "Pending"]);
}
