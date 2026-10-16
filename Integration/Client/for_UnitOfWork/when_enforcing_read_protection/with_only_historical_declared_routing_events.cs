// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.with_only_historical_declared_routing_events.context;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection;

[Collection(ChronicleCollection.Name)]
public class with_only_historical_declared_routing_events(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_read_with_an_old_routing_tail(fixture)
    {
        async Task Because()
        {
            CreateUnit();
            using (var frame = Unit.EnforceReadProtection())
            {
                await Acquire();
                await CaptureRoutingTail();
                StageDeclared();
                await Commit();
            }
            Count = await StoredCount();
        }
    }

    [Fact] void should_have_a_filtered_tail_far_below_the_read_boundary() => Context.RoutingTail.Value.ShouldEqual(0UL);
    [Fact] void should_keep_the_global_read_boundary_instead_of_the_filtered_tail() => Context.Unit.GetAppendConditions().Single().After.Value.ShouldEqual(100UL);
    [Fact] void should_not_conflict_on_historical_events() => Context.Unit.IsSuccess.ShouldBeTrue();
    [Fact] void should_store_the_planned_event() => Context.Count.ShouldEqual(3);
}
