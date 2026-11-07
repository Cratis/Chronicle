// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.AggregateRoots;
using context = Cratis.Chronicle.Integration.for_ProjectionChangesetNotifier.when_a_child_is_removed_with_join.and_it_was_the_last_child.context;

namespace Cratis.Chronicle.Integration.for_ProjectionChangesetNotifier.when_a_child_is_removed_with_join;

[Collection(ChronicleCollection.Name)]
public class and_it_was_the_last_child(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_watched_users_in_groups<WatchedUserProjection>(fixture)
    {
        public EventSourceId UserA = EventSourceId.New();
        public EventSourceId GroupG = EventSourceId.New();

        async Task Because()
        {
            await AppendSetup(
                UserA,
                new EventAndEventSourceId(UserA, new UserCreated("Ada")),
                new EventAndEventSourceId(GroupG, new UserAddedToGroup(UserA)));

            await AppendAndHandle(GroupG, new GroupRemoved());
            await WaitForCount(1);
        }
    }

    [Fact] void should_notify_the_parent_once() => Context.Changesets.Count(_ => _.ModelKey.Value == Context.UserA.Value).ShouldEqual(1);
    [Fact] void should_notify_nothing_else() => Context.Changesets.Count.ShouldEqual(1);
    [Fact] void should_have_a_read_model() => Context.Changesets.Single().ReadModel.ShouldNotBeNull();
    [Fact] void should_have_no_groups() => Context.Changesets.Single().ReadModel!.Groups.ShouldBeEmpty();
    [Fact] void should_keep_the_name() => Context.Changesets.Single().ReadModel!.Name.ShouldEqual("Ada");
    [Fact] void should_not_be_removed() => Context.Changesets.Single().Removed.ShouldBeFalse();
}
