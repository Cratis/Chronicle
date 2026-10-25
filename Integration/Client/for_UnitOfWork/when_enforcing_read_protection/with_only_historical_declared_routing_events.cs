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

    /// <summary>
    /// A kernel without conjunction support gets one merged guard; one with it gets the read guard plus a routing guard
    /// narrowed to the declared stream. Either way the read guard keeps the global read boundary.
    /// </summary>
    [Fact] void should_keep_the_global_read_boundary_instead_of_the_filtered_tail() => Context.Unit.GetAppendConditions().Single(_ => _.Reads.Count > 0).After.Value.ShouldEqual(100UL);
    [Fact] void should_narrow_any_separate_routing_guard_to_the_declared_stream() => Context.Unit.GetAppendConditions().Where(_ => _.Reads.Count == 0).ShouldEachConformTo(_ => _.EventStreamType == "numbering");
    [Fact] void should_not_conflict_on_historical_events() => Context.Unit.IsSuccess.ShouldBeTrue();
    [Fact] void should_store_the_planned_event() => Context.Count.ShouldEqual(3);
}
