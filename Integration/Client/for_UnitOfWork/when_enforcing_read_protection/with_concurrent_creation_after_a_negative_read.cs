// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using context = Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.with_concurrent_creation_after_a_negative_read.context;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection;

public abstract class with_concurrent_creation_after_a_negative_read<TContext>(TContext context) : Given<TContext>(context)
    where TContext : with_concurrent_creation_after_a_negative_read.context
{
    [Fact] public void should_start_from_an_absent_instance() => Context.Read.Exists.ShouldBeFalse();
    [Fact] public void should_conflict_on_creation() => Context.Unit.IsSuccess.ShouldBeFalse();
    [Fact] public void should_not_store_the_stale_write() => Context.Count.ShouldEqual(1);
}

[Collection(ChronicleCollection.Name)]
public class with_concurrent_creation_after_a_negative_read(context context) : with_concurrent_creation_after_a_negative_read<context>(context)
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

    [Fact] void should_take_the_conjunction_route() => Context.AdvertisedConjunctions.ShouldBeTrue();
    [Fact] void should_keep_the_negative_condition() => Context.Unit.GetAppendConditions().Single().ExpectsNoMatchingEvent.ShouldBeTrue();
}
