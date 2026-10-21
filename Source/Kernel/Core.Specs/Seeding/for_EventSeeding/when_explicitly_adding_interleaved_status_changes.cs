// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_explicitly_adding_interleaved_status_changes : given.overlapping_global_seed_groups
{
    async Task Because()
    {
        await _grain.SeedGlobalWithResult([StatusChanged("Open"), StatusChanged("Closed"), StatusChanged("Open")], false, "program");
        await ApplyToFutureNamespace();
    }

    [Fact] void should_append_existing_namespace_events_in_offered_order() => AppendedStatuses(_existingEvents).ShouldEqual(["Open", "Closed", "Open"]);
    [Fact] void should_append_future_namespace_events_in_offered_order() => AppendedStatuses(_futureEvents).ShouldEqual(["Open", "Closed", "Open"]);
}
