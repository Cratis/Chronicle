// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.AggregateRoots;
using context = Cratis.Chronicle.Integration.for_ProjectionChangesetNotifier.when_a_child_is_removed_with_join.and_the_root_is_removed.context;

namespace Cratis.Chronicle.Integration.for_ProjectionChangesetNotifier.when_a_child_is_removed_with_join;

[Collection(ChronicleCollection.Name)]
public class and_the_root_is_removed(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_watched_users_in_groups<WatchedUserProjectionWithRootRemoval>(fixture)
    {
        public EventSourceId UserA = EventSourceId.New();
        public EventSourceId UserC = EventSourceId.New();
        public EventSourceId GroupG = EventSourceId.New();
        public int ChangesetsAfterTheRemoval;

        async Task Because()
        {
            await AppendSetup(
                UserA,
                new EventAndEventSourceId(UserA, new UserCreated("Ada")),
                new EventAndEventSourceId(GroupG, new UserAddedToGroup(UserA)));

            await AppendAndHandle(UserA, new UserRemoved());
            await WaitFor(_ => _.ModelKey.Value == UserA.Value && _.Removed);
            ChangesetsAfterTheRemoval = Changesets.Count(_ => _.ModelKey.Value == UserA.Value);

            await AppendAndHandle(GroupG, new GroupRemoved());
            await EventStore.EventLog.Append(UserC, new UserCreated("Cleo"));
            await WaitFor(_ => _.ModelKey.Value == UserC.Value);
        }
    }

    [Fact] void should_report_the_removal_of_the_user() => Context.Changesets.Any(_ => _.ModelKey.Value == Context.UserA.Value && _.Removed).ShouldBeTrue();
    [Fact] void should_not_notify_the_removed_user_again() => Context.Changesets.Count(_ => _.ModelKey.Value == Context.UserA.Value).ShouldEqual(Context.ChangesetsAfterTheRemoval);
    [Fact] void should_not_notify_the_removed_group() => Context.Changesets.Any(_ => _.ModelKey.Value == Context.GroupG.Value).ShouldBeFalse();
}
