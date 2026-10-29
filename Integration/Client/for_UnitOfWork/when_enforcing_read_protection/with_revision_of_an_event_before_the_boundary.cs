// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.with_revision_of_an_event_before_the_boundary.context;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection;

[Collection(ChronicleCollection.Name)]
public class with_revision_of_an_event_before_the_boundary(context context) : Given<context>(context)
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
                await Independent(() => EventStore.EventLog.Revise(first.SequenceNumber, new given.StockSet(0)));
                await WaitForHistoryMutation();
                Stage();
                await Commit();
            }
            Count = await StoredCount();
        }
    }

    [Fact] void should_start_from_the_original_value() => Context.Read.Instance!.Quantity.ShouldEqual(1);
    [Fact] void should_refuse_the_commit_after_a_history_mutation() => Context.Unit.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_a_decision_conflict() => Context.Unit.GetDecisionConflicts().ShouldNotBeEmpty();
    [Fact] void should_not_append_the_staged_event() => Context.Count.ShouldEqual(1);
}
