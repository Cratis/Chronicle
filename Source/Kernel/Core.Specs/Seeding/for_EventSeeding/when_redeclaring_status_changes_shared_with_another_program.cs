// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Seeding;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_redeclaring_status_changes_shared_with_another_program : given.overlapping_global_seed_groups
{
    async Task Establish()
    {
        await _grain.SeedGlobalWithResult([StatusChanged("Open"), StatusChanged("Closed")], true, "first");
        await _grain.SeedGlobalWithResult([StatusChanged("Closed")], true, "second");
        _grainFactory.ClearReceivedCalls();
    }

    async Task Because()
    {
        await _grain.SeedGlobalWithResult([StatusChanged("Open"), StatusChanged("Closed")], true, "first");
        await ApplyToFutureNamespace();
    }

    [Fact] void should_preserve_declared_order_in_future_namespaces() => AppendedStatuses(_futureEvents).ShouldEqual(["Open", "Closed"]);
    [Fact] void should_not_dispatch_to_existing_namespaces() => _grainFactory.DidNotReceive().GetGrain<IResultAwareEventSeeding>(EventSeedingKey.ForNamespace("TestEventStore", "existing").ToString());
}
