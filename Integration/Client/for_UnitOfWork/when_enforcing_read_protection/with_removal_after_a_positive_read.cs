// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.with_removal_after_a_positive_read.context;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection;

[Collection(ChronicleCollection.Name)]
public class with_removal_after_a_positive_read(context context) : Given<context>(context)
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
                await Independent(() => EventStore.EventLog.Append(Source, new given.StockRemoved()));
                Stage();
                await Commit();
            }
            Count = await StoredCount();
        }
    }

    [Fact] void should_start_from_a_present_instance() => Context.Read.Instance!.Quantity.ShouldEqual(1);
    [Fact] void should_watch_the_removal_type() => Context.Unit.GetAppendConditions().Single().EventTypes.Select(_ => _.Id).ShouldContain(Context.EventStore.EventTypes.GetEventTypeFor(typeof(given.StockRemoved)).Id);
    [Fact] void should_conflict_on_removal() => Context.Unit.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_store_the_stale_write() => Context.Count.ShouldEqual(2);
}
