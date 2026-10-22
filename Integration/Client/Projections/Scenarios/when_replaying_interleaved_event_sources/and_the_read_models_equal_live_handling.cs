// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Projections.Concepts;
using Cratis.Chronicle.Integration.Projections.Events;
using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.Events;
using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.ReadModels;
using Cratis.Chronicle.Integration.Projections.Scenarios.ReadModels;
using Cratis.Chronicle.Jobs;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_replaying_interleaved_event_sources.and_the_read_models_equal_live_handling.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_replaying_interleaved_event_sources;

[Collection(ChronicleCollection.Name)]
public class and_the_read_models_equal_live_handling(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        readonly Guid[] _modules = Enumerable.Range(0, 8).Select(_ => Guid.NewGuid()).ToArray();
        readonly Guid[] _features = Enumerable.Range(0, 8).Select(_ => Guid.NewGuid()).ToArray();
        readonly Guid[] _slices = Enumerable.Range(0, 8).Select(_ => Guid.NewGuid()).ToArray();
        readonly Guid[] _groups = Enumerable.Range(0, 13).Select(_ => Guid.NewGuid()).ToArray();
        readonly Guid[] _users = Enumerable.Range(0, 13).Select(_ => Guid.NewGuid()).ToArray();

        public ModuleReadModel[] LiveModules;
        public ModuleReadModel[] ReplayedModules;
        public Group[] LiveGroups;
        public Group[] ReplayedGroups;
        public Member[] LiveMembers;
        public Member[] ReplayedMembers;
        public (string Id, EventSequenceNumber LastHandledEventSequenceNumber, ulong HandledEventCount)[] LiveObservers;
        public (string Id, EventSequenceNumber LastHandledEventSequenceNumber, ulong HandledEventCount)[] ReplayedObservers;

        public override IEnumerable<Type> EventTypes =>
        [
            typeof(ModuleAdded), typeof(FeatureAddedToModule), typeof(SliceAddedToFeature), typeof(EventAddedToSlice),
            typeof(CommandSetOnSlice), typeof(CommandRenamedOnSlice), typeof(CommandClearedFromSlice),
            typeof(GroupCreated), typeof(UserCreated), typeof(UserAddedToGroup), typeof(UserRemovedFromGroup)
        ];

        public override IEnumerable<Type> Projections => [typeof(GroupProjection)];
        public override IEnumerable<Type> ModelBoundProjections =>
            [typeof(ModuleReadModel), typeof(FeatureItem), typeof(SliceItem), typeof(SliceCommandItem), typeof(EventItem), typeof(Member)];

        protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(new GroupProjection());

        async Task Because()
        {
            var handlers = EventStore.Projections.GetAllHandlers().ToArray();
            foreach (var handler in handlers) await handler.WaitTillSubscribed();

            await AppendInterleavedEvents();
            var projectionIds = handlers.Select(h => h.Id.ToString()).ToHashSet();
            LiveObservers = (await EventStore.Observers.GetAll()).Where(o => projectionIds.Contains(o.Id.ToString()))
                .Select(o => (o.Id.ToString(), o.LastHandledEventSequenceNumber, o.HandledEventCount)).ToArray();
            LiveModules = await ReadAll<ModuleReadModel>(_modules);
            LiveGroups = await ReadAll<Group>(_groups);
            LiveMembers = await ReadAll<Member>(_users);

            foreach (var handler in handlers)
            {
                var job = await EventStore.Projections.Replay(handler.Id);
                await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(job);
                await handler.WaitTillActive();
            }

            ReplayedModules = await ReadAll<ModuleReadModel>(_modules);
            ReplayedGroups = await ReadAll<Group>(_groups);
            ReplayedMembers = await ReadAll<Member>(_users);

            // Replay completion precedes the coalesced handled-count write. Poll the reported counts, not the clock.
            var deadline = DateTime.UtcNow.AddSeconds(new Configuration.Observers().WriteCoalescingInterval + 5);
            while (true)
            {
                ReplayedObservers = (await EventStore.Observers.GetAll()).Where(o => projectionIds.Contains(o.Id.ToString()))
                    .Select(o => (o.Id.ToString(), o.LastHandledEventSequenceNumber, o.HandledEventCount)).ToArray();
                if (LiveObservers.All(live => ReplayedObservers.Any(replayed => replayed.Id == live.Id && replayed.HandledEventCount == live.HandledEventCount)) ||
                    DateTime.UtcNow >= deadline)
                {
                    break;
                }

                await Task.Delay(50);
            }
        }

        async Task AppendInterleavedEvents()
        {
            var lastMember = EventSequenceNumber.Unavailable;
            for (var i = 0; i < _users.Length; i++)
                lastMember = (await EventStore.EventLog.Append(_users[i], new UserCreated($"User {i}"))).SequenceNumber;
            await HandlerFor<Member>().WaitTillReachesEventSequenceNumber(lastMember);
            for (var i = 0; i < _modules.Length; i++)
                await EventStore.EventLog.Append(_modules[i], new ModuleAdded($"Module {i}"));
            for (var i = 0; i < _groups.Length; i++)
            {
                await EventStore.EventLog.Append(_groups[i], new GroupCreated($"Group {i}"));
                await EventStore.EventLog.Append(_groups[i], new UserAddedToGroup((UserId)_users[i]));
                await EventStore.EventLog.Append(_groups[i], new UserAddedToGroup((UserId)_users[(i + 1) % _users.Length]));
            }
            for (var i = 0; i < _modules.Length; i++)
                await EventStore.EventLog.Append(_features[i], new FeatureAddedToModule(_modules[i], _features[i], $"Feature {i}"));
            var lastModule = EventSequenceNumber.Unavailable;
            for (var i = 0; i < _modules.Length; i++)
            {
                await EventStore.EventLog.Append(_slices[i], new SliceAddedToFeature(_features[i], _slices[i], $"Slice {i}"));
                lastModule = (await EventStore.EventLog.Append(_slices[i], new EventAddedToSlice(_slices[i], Guid.NewGuid(), $"Event {i}"))).SequenceNumber;
                if (i % 2 == 0)
                    lastModule = (await EventStore.EventLog.Append(_slices[i], new CommandSetOnSlice($"Command {i}", "{}"))).SequenceNumber;
            }
            await HandlerFor<ModuleReadModel>().WaitTillReachesEventSequenceNumber(lastModule);
            var last = EventSequenceNumber.Unavailable;
            for (var i = 0; i < _groups.Length; i++)
                last = (await EventStore.EventLog.Append(_groups[i], new UserRemovedFromGroup((UserId)_users[i]))).SequenceNumber;
            await HandlerFor<Group>().WaitTillReachesEventSequenceNumber(last);
        }

        IProjectionHandler HandlerFor<T>() => EventStore.Projections.GetAllHandlers()
            .Single(h => h.Id == EventStore.Projections.GetProjectionIdForModel<T>());

        async Task<T[]> ReadAll<T>(IEnumerable<Guid> ids)
        {
            var documents = new List<T>();
            foreach (var id in ids) documents.Add(await EventStore.ReadModels.GetInstanceById<T>(id.ToString()));
            return documents.ToArray();
        }
    }

    [Fact] void should_rebuild_every_hierarchy_field() => Assert.Equivalent(Context.LiveModules, Context.ReplayedModules, strict: true);
    [Fact] void should_rebuild_every_joined_and_removed_child_field() => Assert.Equivalent(Context.LiveGroups, Context.ReplayedGroups, strict: true);
    [Fact] void should_rebuild_every_event_source_keyed_field() => Assert.Equivalent(Context.LiveMembers, Context.ReplayedMembers, strict: true);

    [Fact]
    void should_have_materialized_the_live_hierarchies()
    {
        Context.LiveModules.Length.ShouldEqual(8);
        foreach (var module in Context.LiveModules)
        {
            module.ShouldNotBeNull();
            module.Features.Single().Slices.Single().Events.Count().ShouldEqual(1);
        }
    }

    [Fact]
    void should_have_joined_and_removed_live_children()
    {
        Context.LiveGroups.Length.ShouldEqual(13);
        foreach (var group in Context.LiveGroups) group.Users.Single().Name.ShouldNotBeEmpty();
        Context.LiveMembers.Length.ShouldEqual(13);
        foreach (var member in Context.LiveMembers) member.Name.ShouldNotBeEmpty();
    }

    [Fact]
    void should_preserve_observer_bookkeeping()
    {
        Context.LiveObservers.Length.ShouldEqual(3);
        foreach (var live in Context.LiveObservers)
        {
            var replayed = Context.ReplayedObservers.Single(o => o.Id == live.Id);
            live.HandledEventCount.ShouldBeGreaterThan(0UL);
            replayed.LastHandledEventSequenceNumber.ShouldEqual(live.LastHandledEventSequenceNumber);
            replayed.HandledEventCount.ShouldEqual(live.HandledEventCount);
        }
    }
}
