// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.for_UnitOfWork.when_committing_several_guards_for_one_event_source.and_every_guard_holds.context;

namespace Cratis.Chronicle.Integration.for_UnitOfWork.when_committing_several_guards_for_one_event_source;

[Collection(ChronicleCollection.Name)]
public class and_every_guard_holds(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.several_guards_for_one_event_source(fixture)
    {
        async Task Because()
        {
            CreateUnit();
            Stage();
            await Commit();
            Count = await StoredCount();
        }
    }

    [Fact] void should_take_the_conjunction_route() => Context.AdvertisedConjunctions.ShouldBeTrue();
    [Fact] void should_accept_the_conjunction() => Context.Error.ShouldBeNull();
    [Fact] void should_commit_successfully() => Context.Unit.IsSuccess.ShouldBeTrue();
    [Fact] void should_store_the_planned_event() => Context.Count.ShouldEqual(3);
}
