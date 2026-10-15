// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

using context = Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.with_an_explicit_boundary_captured_after_the_read.context;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection;

[Collection(ChronicleCollection.Name)]
public class with_an_explicit_boundary_captured_after_the_read(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_read_protection_scenario(fixture)
    {
        public int Count;

        async Task Because()
        {
            await EventStore.EventLog.Append(Source, new given.StockSet(1));
            CreateUnit();
            using (var frame = Unit.EnforceReadProtection())
            {
                await Acquire();
                await Independent(() => EventStore.EventLog.Append(Source, new given.StockSet(0)));
                using (var reads = Unit.AllowReads(ReadAllowance.Contributed, "capture exact source tail"))
                {
                    var later = await EventStore.EventLog.GetTailSequenceNumber(Source);
                    Stage(new ConcurrencyScope(later, Source));
                }
                Error = await Catch.Exception(Commit);
            }
            Count = await StoredCount();
        }
    }

    [Fact] void should_preserve_the_stale_decision_value() => Context.Read.Instance!.Quantity.ShouldEqual(1);
    [Fact] void should_refuse_the_later_boundary_even_with_a_covering_predicate() => Context.Error.ShouldBeOfExactType<ExplicitScopeDoesNotCoverReads>();
    [Fact] void should_not_produce_an_append_plan() => Context.Unit.GetAppendConditions().ShouldBeEmpty();
    [Fact] void should_not_store_the_stale_write() => Context.Count.ShouldEqual(2);
}
