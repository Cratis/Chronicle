// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_replaying_after_parent_key_changes.and_the_children_keep_their_live_roots.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_replaying_after_parent_key_changes;

[Collection(ChronicleCollection.Name)]
public class and_the_children_keep_their_live_roots(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        readonly string _parent = Guid.NewGuid().ToString();
        public string[] Roots = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid().ToString()).ToArray();
        public string[] Children = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid().ToString()).ToArray();
        public Root[] Live;
        public Root[] Replayed;

        public override IEnumerable<Type> EventTypes => [typeof(ParentAssigned), typeof(ChildRecorded)];
        public override IEnumerable<Type> Projections => [typeof(RootProjection)];

        protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(new RootProjection());

        async Task Because()
        {
            var handler = EventStore.Projections.GetHandlerFor<RootProjection>();
            await handler.WaitTillSubscribed();
            await handler.WaitTillActive();
            var last = EventSequenceNumber.First;
            for (var index = 0; index < Roots.Length; index++)
            {
                await EventStore.EventLog.Append(_parent, new ParentAssigned(Roots[index]));
                var appended = await EventStore.EventLog.Append(Children[index], new ChildRecorded(_parent, Children[index], $"Child {index}"));
                await handler.WaitTillReachesEventSequenceNumber(appended.SequenceNumber);
                last = appended.SequenceNumber;
            }

            Live = await ReadRoots();
            var job = await EventStore.Projections.Replay(handler.Id);
            await EventStore.Jobs.WaitTillJobCompletesOrIsDeleted(job);
            await handler.WaitTillActive();
            await handler.WaitTillReachesEventSequenceNumber(last);
            Replayed = await ReadRoots();
        }

        async Task<Root[]> ReadRoots()
        {
            var result = new List<Root>();
            foreach (var root in Roots)
            {
                result.Add(await ReadRoot(root));
            }

            return result.ToArray();
        }

        async Task<Root> ReadRoot(string id) =>
            await EventStore.ReadModels.GetInstanceById<Root>(id) ?? throw new InvalidOperationException($"Root '{id}' was not projected.");
    }

    [Fact]
    void should_have_materialized_each_live_child_under_its_current_parent_key()
    {
        Context.Live.Length.ShouldEqual(3);
        for (var index = 0; index < Context.Roots.Length; index++)
        {
            var root = Context.Live[index];
            root.ShouldNotBeNull();
            root.RootId.ShouldEqual(Context.Roots[index]);
            root.Children.Single().ChildId.ShouldEqual(Context.Children[index]);
            root.Children.Single().Name.ShouldEqual($"Child {index}");
        }
    }

    [Fact] void should_rebuild_the_same_roots_and_children_through_the_replay_pipeline() => Assert.Equivalent(Context.Live, Context.Replayed, strict: true);
}
