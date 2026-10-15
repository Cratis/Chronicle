// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.with_concurrent_creation_after_a_negative_read.context;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection;

[Collection(ChronicleCollection.Name)]
public class with_concurrent_creation_after_a_negative_read(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_read_protection_scenario(fixture)
    {
        public int Count;

        async Task Because()
        {
            CreateUnit();
            using (var frame = Unit.EnforceReadProtection())
            {
                await Acquire();
                await Independent(() => EventStore.EventLog.Append(Source, new given.StockSet(1)));
                Stage();
                await Commit();
            }
            Count = await StoredCount();
        }
    }

    [Fact] void should_start_from_an_absent_instance() => Context.Read.Exists.ShouldBeFalse();
    [Fact] void should_conflict_on_creation() => Context.Unit.IsSuccess.ShouldBeFalse();
    [Fact] void should_keep_the_negative_condition() => Context.Unit.GetAppendConditions().Single().ExpectsNoMatchingEvent.ShouldBeTrue();
    [Fact] void should_not_store_the_stale_write() => Context.Count.ShouldEqual(1);
}
