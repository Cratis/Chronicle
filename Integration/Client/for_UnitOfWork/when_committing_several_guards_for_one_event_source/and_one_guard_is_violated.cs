// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using context = Cratis.Chronicle.Integration.for_UnitOfWork.when_committing_several_guards_for_one_event_source.and_one_guard_is_violated.context;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_committing_several_guards_for_one_event_source;

public abstract class and_one_guard_is_violated<TContext>(TContext context) : Given<TContext>(context)
    where TContext : and_one_guard_is_violated.context
{
    [Fact] public void should_not_store_the_stale_write() => Context.Count.ShouldEqual(3);
}

[Collection(ChronicleCollection.Name)]
public class and_one_guard_is_violated(context context) : and_one_guard_is_violated<context>(context)
{
    public class context(ChronicleFixture fixture) : given.several_guards_for_one_event_source(fixture)
    {
        async Task Because()
        {
            CreateUnit();
            Stage();
            await Independent(() => EventStore.EventLog.Append(Source, new given.Corrected(), eventStreamType: "corrections", eventStreamId: "open"));
            await Commit();
            Count = await StoredCount();
        }
    }

    [Fact] void should_take_the_conjunction_route() => Context.AdvertisedConjunctions.ShouldBeTrue();
    [Fact] void should_report_the_kernel_rejection_without_throwing() => Context.Error.ShouldBeNull();
    [Fact] void should_refuse_the_stale_conjunction() => Context.Unit.IsSuccess.ShouldBeFalse();
    [Fact] void should_identify_the_guarded_source() => Context.Unit.GetConcurrencyViolations().Single().EventSourceId.ShouldEqual(Context.Source);
}
