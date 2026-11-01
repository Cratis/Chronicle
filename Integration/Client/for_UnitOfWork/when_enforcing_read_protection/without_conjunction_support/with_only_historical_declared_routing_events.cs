// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.without_conjunction_support.with_only_historical_declared_routing_events.context;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.without_conjunction_support;

[Collection(ChronicleCollection.Name)]
public class with_only_historical_declared_routing_events(context context) : when_enforcing_read_protection.with_only_historical_declared_routing_events<context>(context)
{
    public class context(ChronicleFixture fixture) : when_enforcing_read_protection.with_only_historical_declared_routing_events.context(fixture)
    {
        public override bool KernelSupportsConjunctions => false;
    }

    [Fact] void should_take_the_fallback_route() => Context.AdvertisedConjunctions.ShouldBeFalse();
    [Fact] void should_send_one_merged_guard() => Context.Unit.GetAppendConditions().Count.ShouldEqual(1);
    [Fact] void should_cover_the_routing_stream() => Context.Unit.GetAppendConditions().Single().EventStreamType.ShouldBeNull();
}
