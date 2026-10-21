// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.with_a_newer_generation_after_the_read.context;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection;

[Collection(ChronicleCollection.Name)]
public class with_a_newer_generation_after_the_read(context context) : Given<context>(context)
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
                await Independent(() => EventStore.EventLog.Append(Source, new given.StockSetV2(0)));
                Stage();
                await Commit();
            }
            Count = await StoredCount();
        }
    }

    [Fact] void should_read_the_first_generation() => Context.Read.Instance!.Quantity.ShouldEqual(1);
    [Fact] void should_conflict_by_type_id_across_generations() => Context.Unit.IsSuccess.ShouldBeFalse();
    [Fact] void should_identify_the_dependency_source() => Context.Unit.GetConcurrencyViolations().Single().EventSourceId.ShouldEqual(Context.Source);
    [Fact] void should_not_store_the_stale_write() => Context.Count.ShouldEqual(2);
}
