// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

using context = Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.with_a_stream_scope_for_a_whole_source_read.context;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection;

[Collection(ChronicleCollection.Name)]
public class with_a_stream_scope_for_a_whole_source_read(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_read_protection_scenario(fixture)
    {
        public int Count;

        async Task Because()
        {
            var first = await EventStore.EventLog.Append(Source, new given.StockSet(1));
            CreateUnit();
            using (var frame = Unit.EnforceReadProtection())
            {
                await Acquire();
                Stage(new ConcurrencyScope(first.SequenceNumber, Source, "approval", "january"));
                Error = await Catch.Exception(Commit);
            }
            Count = await StoredCount();
        }
    }

    [Fact] void should_refuse_a_stream_condition_that_misses_the_model() => Context.Error.ShouldBeOfExactType<ExplicitScopeDoesNotCoverReads>();
    [Fact] void should_not_produce_an_append_plan() => Context.Unit.GetAppendConditions().ShouldBeEmpty();
    [Fact] void should_not_store_the_planned_event() => Context.Count.ShouldEqual(1);
}
