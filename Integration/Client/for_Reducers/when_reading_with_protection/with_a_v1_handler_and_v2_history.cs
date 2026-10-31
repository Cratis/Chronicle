// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using context = Cratis.Chronicle.Integration.for_Reducers.when_reading_with_protection.with_a_v1_handler_and_v2_history.context;
using stock = Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.given;

namespace Cratis.Chronicle.Integration.for_Reducers.when_reading_with_protection;

[Collection(ChronicleCollection.Name)]
public class with_a_v1_handler_and_v2_history(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_reducer_decision_scenario(fixture)
    {
        public DecisionRead<given.Stock> Read;
        public DecisionRead<given.StaticStock> StaticRead;
        public int StoredCount;

        async Task Because()
        {
            await EventStore.EventLog.Append(Source, new stock.StockSet(10));

            // Row 17: the debit is before the decision boundary and is stored as V2.
            await EventStore.EventLog.Append(Source, new stock.StockSetV2(0));
            CreateUnit();
            using (var frame = Unit.EnforceReadProtection())
            {
                Read = await EventStore.GetDecisionReads().GetFor<given.Stock>(Unit, Source);
                StaticRead = await EventStore.GetDecisionReads().GetFor<given.StaticStock>(Unit, Source);
                await Independent(() => EventStore.EventLog.Append(Source, new stock.StockSetV2(-1)));
                Unit.AddEvent(EventSequenceId.Log, Source, new stock.StockSet(2), Causation.Unknown());
                await Unit.CommitAsOwner(Owner);
            }
            StoredCount = (await EventStore.EventLog.GetFromSequenceNumber(Events.EventSequenceNumber.First, Source)).Count;
        }
    }

    [Fact] void should_select_v1_content_for_every_event() => Context.Read.Instance!.Quantity.ShouldEqual(0);
    [Fact] void should_dispatch_the_event_that_would_otherwise_be_skipped() => Context.Read.Instance!.Handled.ShouldEqual(2);
    [Fact] void should_supply_the_declared_context_generation() => Context.Read.Instance!.Generation.ShouldEqual(1U);
    [Fact] void should_apply_the_same_generation_selection_to_static_reducers() => Context.StaticRead.Instance!.Quantity.ShouldEqual(0);
    [Fact] void should_dispatch_both_events_for_static_reducers() => Context.StaticRead.Instance!.Handled.ShouldEqual(2);
    [Fact] void should_guard_by_type_id_against_a_newer_generation() => Context.Unit.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_write_the_stale_decision() => Context.StoredCount.ShouldEqual(3);
}
