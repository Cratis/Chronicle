// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_declaring_interleaved_status_changes : given.overlapping_global_seed_groups
{
    Task Because() => _grain.SeedGlobalWithResult([StatusChanged("Open"), StatusChanged("Closed"), StatusChanged("Open")], true, "program");

    [Fact] void should_append_existing_namespace_events_in_declared_order() => AppendedStatuses(_existingEvents).ShouldEqual(["Open", "Closed", "Open"]);
}
