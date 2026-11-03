// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.without_conjunction_support.with_covering_explicit_and_source_less_routing.context;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.without_conjunction_support;

[Collection(ChronicleCollection.Name)]
public class with_covering_explicit_and_source_less_routing(context context) : when_enforcing_read_protection.with_covering_explicit_and_source_less_routing<context>(context)
{
    public class context(ChronicleFixture fixture) : when_enforcing_read_protection.with_covering_explicit_and_source_less_routing.context(fixture)
    {
        public override bool KernelSupportsConjunctions => false;
    }

    [Fact] void should_take_the_fallback_route() => Context.AdvertisedConjunctions.ShouldBeFalse();
    [Fact] void should_keep_the_explicit_source_boundary() => Context.Unit.GetAppendConditions().Single(_ => _.EventSourceId is not null).After.Value.ShouldEqual(0UL);
    [Fact] void should_keep_declared_routing_on_a_separate_internal_label() => Context.Unit.GetAppendConditions().Single(_ => _.EventSourceId is null).Label.ShouldEqual($"$read-protection/routing/{Context.Source}/0");
    [Fact] void should_keep_the_source_less_filtered_boundary() => Context.Unit.GetAppendConditions().Single(_ => _.EventSourceId is null).After.Value.ShouldEqual(1UL);
}
