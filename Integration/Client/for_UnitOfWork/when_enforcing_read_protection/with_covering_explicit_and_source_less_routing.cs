// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Transactions;

using context = Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.with_covering_explicit_and_source_less_routing.context;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection;

[Collection(ChronicleCollection.Name)]
public class with_covering_explicit_and_source_less_routing(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_read_protection_scenario(fixture)
    {
        public int Count;

        async Task Because()
        {
            await EventStore.EventLog.Append(Source, new given.StockSet(1));
            await EventStore.EventLog.Append("numbering-source", new given.RoutingFact(), eventStreamType: "numbering");
            CreateUnit();
            using (var frame = Unit.EnforceReadProtection())
            {
                await Acquire();
                Stage(options: new EventEnrollmentOptions
                {
                    ConcurrencyScope = new ConcurrencyScope(0, Source), EventStreamType = "numbering",
                    DeclaredConcurrency = ConcurrencyDimensions.EventStreamType
                });
                Error = await Catch.Exception(Commit);
            }
            Count = await StoredCount();
        }
    }

    [Fact] void should_commit_against_the_real_kernel() => Context.Error.ShouldBeNull();
    [Fact] void should_accept_both_independent_conditions() => Context.Unit.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_the_explicit_source_boundary() => Context.Unit.GetAppendConditions().Single(_ => _.EventSourceId is not null).After.Value.ShouldEqual(0UL);
    [Fact] void should_keep_declared_routing_on_a_separate_internal_label() => Context.Unit.GetAppendConditions().Single(_ => _.EventSourceId is null).Label.ShouldEqual($"$read-protection/routing/{Context.Source}/0");
    [Fact] void should_keep_the_source_less_filtered_boundary() => Context.Unit.GetAppendConditions().Single(_ => _.EventSourceId is null).After.Value.ShouldEqual(1UL);
    [Fact] void should_store_the_planned_event() => Context.Count.ShouldEqual(2);
}
