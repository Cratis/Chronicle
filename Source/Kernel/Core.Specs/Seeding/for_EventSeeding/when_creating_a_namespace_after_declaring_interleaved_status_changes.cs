// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_creating_a_namespace_after_declaring_interleaved_status_changes : given.overlapping_global_seed_groups
{
    Task Establish() => _grain.SeedGlobalWithResult([StatusChanged("Open"), StatusChanged("Closed"), StatusChanged("Open")], true, "program");

    Task Because() => ApplyToFutureNamespace();

    [Fact] void should_append_future_namespace_events_in_declared_order() => AppendedStatuses(_futureEvents).ShouldEqual(["Open", "Closed", "Open"]);
}
