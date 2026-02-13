// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_deferred_parent_resolution.Concepts;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_deferred_parent_resolution.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_deferred_parent_resolution.ReadModels;
using Cratis.Chronicle.Observation;
using MongoDB.Driver;
using context = Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_projecting_with_deferred_parent_resolution.and_resolves_through_second_event_type.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_deferred_parent_resolution;

[Collection(ChronicleCollection.Name)]
public class and_resolves_through_second_event_type(context context) : Given<context>(context)
{
    const string RootName = "Test Root";
    const string UpdatedRootName = "Updated Root";
    const string ChildName = "Test Child";
    const string UpdatedChildName = "Updated Child Name";

    public class context(ChronicleInProcessFixture fixture) : Specification(fixture)
    {
        public RootId RootId;
        public ChildId ChildId;
        public IEnumerable<FailedPartition> FailedPartitions = [];
        public Root Result;
        public EventSequenceNumber LastEventSequenceNumber = EventSequenceNumber.First;

        public override IEnumerable<Type> EventTypes => [typeof(RootCreated), typeof(RootUpdated), typeof(ChildAddedToRoot), typeof(ChildNameChanged)];
        public override IEnumerable<Type> Projections => [typeof(RootProjection)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(new RootProjection());
        }

        void Establish()
        {
            RootId = RootId.New();
            ChildId = ChildId.New();
        }

        async Task Because()
        {
            var projection = EventStore.Projections.GetHandlerFor<RootProjection>();
            await projection.WaitTillActive();

            // Event 1: ChildNameChanged arrives FIRST (deferred)
            var appendResult = await EventStore.EventLog.Append(RootId, new ChildNameChanged(ChildId, "First Update"));
            LastEventSequenceNumber = appendResult.SequenceNumber;
            await projection.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            // Event 2: RootCreated - this also gets deferred for the child (because ChildId != RootId)
            appendResult = await EventStore.EventLog.Append(RootId, new RootCreated(RootName));
            LastEventSequenceNumber = appendResult.SequenceNumber;
            await projection.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            // Event 3: RootUpdated - this is a second event type for root, should also be considered
            appendResult = await EventStore.EventLog.Append(RootId, new RootUpdated(UpdatedRootName));
            LastEventSequenceNumber = appendResult.SequenceNumber;
            await projection.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            // Event 4: ChildAddedToRoot - resolves and creates child
            appendResult = await EventStore.EventLog.Append(RootId, new ChildAddedToRoot(RootId, ChildId, ChildName));
            LastEventSequenceNumber = appendResult.SequenceNumber;
            await projection.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            // Event 5: Another ChildNameChanged - should resolve by finding RootUpdated (or RootCreated)
            appendResult = await EventStore.EventLog.Append(RootId, new ChildNameChanged(ChildId, UpdatedChildName));
            LastEventSequenceNumber = appendResult.SequenceNumber;
            await projection.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            // Give it some time to process
            await Task.Delay(1000);

            // Get failures if any
            FailedPartitions = await projection.GetFailedPartitions();

            // Get the result
            var collection = ChronicleFixture.ReadModels.Database.GetCollection<Root>();
            var queryResult = await collection.FindAsync(filter => filter.Name == UpdatedRootName);
            Result = queryResult.FirstOrDefault();
        }
    }

    [Fact] void should_have_no_failed_partitions() => Context.FailedPartitions.ShouldBeEmpty();
    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_have_updated_root_name() => Context.Result.Name.ShouldEqual(UpdatedRootName);
    [Fact] void should_have_one_child() => Context.Result.Children.Count.ShouldEqual(1);
    [Fact] void should_have_child_id() => Context.Result.Children[0].ChildId.ShouldEqual(Context.ChildId);
    [Fact] void should_have_updated_child_name() => Context.Result.Children[0].Name.ShouldEqual(UpdatedChildName);
    [Fact] void should_set_the_event_sequence_number_to_last_event() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
