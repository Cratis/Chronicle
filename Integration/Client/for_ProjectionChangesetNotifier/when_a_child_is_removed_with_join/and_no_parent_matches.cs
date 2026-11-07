// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.AggregateRoots;
using context = Cratis.Chronicle.Integration.for_ProjectionChangesetNotifier.when_a_child_is_removed_with_join.and_no_parent_matches.context;

namespace Cratis.Chronicle.Integration.for_ProjectionChangesetNotifier.when_a_child_is_removed_with_join;

[Collection(ChronicleCollection.Name)]
public class and_no_parent_matches(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : given.a_watched_users_in_groups<WatchedUserProjection>(fixture)
    {
        public EventSourceId UserA = EventSourceId.New();
        public EventSourceId UserC = EventSourceId.New();
        public EventSourceId GroupG = EventSourceId.New();

        async Task Because()
        {
            await AppendSetup(
                UserA,
                new EventAndEventSourceId(GroupG, new GroupCreated("G")),
                new EventAndEventSourceId(UserA, new UserCreated("Ada")));

            await AppendAndHandle(GroupG, new GroupRemoved());
            await EventStore.EventLog.Append(UserC, new UserCreated("Cleo"));
            await WaitFor(_ => _.ModelKey.Value == UserC.Value);
        }
    }

    [Fact] void should_not_notify_the_removed_group() => Context.Changesets.Any(_ => _.ModelKey.Value == Context.GroupG.Value).ShouldBeFalse();
    [Fact] void should_not_notify_without_a_read_model() => Context.Changesets.Any(_ => _.ReadModel is null).ShouldBeFalse();
    [Fact] void should_not_notify_with_a_default_name() => Context.Changesets.Any(_ => string.IsNullOrEmpty(_.ReadModel!.Name)).ShouldBeFalse();
}
