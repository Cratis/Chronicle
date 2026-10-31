// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection.with_no_history_mutation_after_the_read.context;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_enforcing_read_protection;

[Collection(ChronicleCollection.Name)]
public class with_no_history_mutation_after_the_read(context context) : Given<context>(context)
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
                Stage();
                await Commit();
            }
            Count = await StoredCount();
        }
    }

    [Fact] void should_commit() => Context.Unit.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_the_staged_event() => Context.Count.ShouldEqual(2);
}
