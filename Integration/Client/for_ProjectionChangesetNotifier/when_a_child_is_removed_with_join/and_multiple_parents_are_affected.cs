// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Integration.AggregateRoots;
using context = Cratis.Chronicle.Integration.for_ProjectionChangesetNotifier.when_a_child_is_removed_with_join.and_multiple_parents_are_affected.context;

namespace Cratis.Chronicle.Integration.for_ProjectionChangesetNotifier.when_a_child_is_removed_with_join;

[Collection(ChronicleCollection.Name)]
public class and_multiple_parents_are_affected(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_watched_users_in_groups<WatchedUserProjection>(fixture)
    {
        public EventSourceId UserA = EventSourceId.New();
        public EventSourceId UserB = EventSourceId.New();
        public EventSourceId GroupG = EventSourceId.New();
        public EventSourceId GroupG2 = EventSourceId.New();

        async Task Because()
        {
            await AppendSetup(
                UserB,
                new EventAndEventSourceId(UserA, new UserCreated("Ada")),
                new EventAndEventSourceId(UserB, new UserCreated("Bob")),
                new EventAndEventSourceId(GroupG, new UserAddedToGroup(UserA)),
                new EventAndEventSourceId(GroupG2, new UserAddedToGroup(UserA)),
                new EventAndEventSourceId(GroupG, new UserAddedToGroup(UserB)));

            await AppendAndHandle(GroupG, new GroupRemoved());
            await WaitForCount(2);
        }
    }

    [Fact] void should_notify_each_parent_once() => Context.Changesets.Count(_ => _.ModelKey.Value == Context.UserA.Value).ShouldEqual(1);
    [Fact] void should_notify_the_other_parent_once() => Context.Changesets.Count(_ => _.ModelKey.Value == Context.UserB.Value).ShouldEqual(1);
    [Fact] void should_not_notify_anything_else() => Context.Changesets.Count.ShouldEqual(2);
    [Fact] void should_not_be_removed() => Context.Changesets.All(_ => !_.Removed).ShouldBeTrue();
    [Fact] void should_be_modified() => Context.Changesets.All(_ => _.ChangeType == ReadModelChangeType.Modified).ShouldBeTrue();
    [Fact] void should_keep_the_other_group_on_the_first_parent() => Context.Changesets.Single(_ => _.ModelKey.Value == Context.UserA.Value).ReadModel!.Groups.Select(_ => _.GroupId.Value).ShouldContainOnly(Context.GroupG2.Value);
    [Fact] void should_leave_the_second_parent_without_groups() => Context.Changesets.Single(_ => _.ModelKey.Value == Context.UserB.Value).ReadModel!.Groups.ShouldBeEmpty();
    [Fact] void should_keep_the_name() => Context.Changesets.Single(_ => _.ModelKey.Value == Context.UserA.Value).ReadModel!.Name.ShouldEqual("Ada");
    [Fact] void should_not_notify_the_removed_group() => Context.Changesets.Any(_ => _.ModelKey.Value == Context.GroupG.Value).ShouldBeFalse();
}
