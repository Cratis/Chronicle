// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_updating_projection_definition.for_materialized_projection_with_deactivated_subscriber.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_updating_projection_definition;

[Collection(ChronicleCollection.Name)]
public class for_materialized_projection_with_deactivated_subscriber(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleInProcessFixture) : given.a_projection_and_events_appended_to_it<MaterializedProjection, TestReadModel>(chronicleInProcessFixture)
    {
        public TestReadModel ResultAfterReplay;

        public override IEnumerable<Type> EventTypes => [typeof(TestEvent)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            MaterializedProjection.MapBothProperties = false;
            services.AddSingleton(new MaterializedProjection());
        }

        void Establish() => EventsToAppend.Add(new TestEvent("First Name", "First Description"));

        async Task Because()
        {
            // Simulate an application restart: deactivate all non-KeepAlive grains so
            // that the ProjectionObserverSubscriber grain loses its in-memory state and
            // the IProjection grain's _definitionObservers list is cleared. When the
            // definition is then changed and the subscriber re-activates during replay,
            // it must pick up the new definition rather than its stale stored state.
            await DeactivateAllGrains();

            MaterializedProjection.MapBothProperties = true;
            await EventStore.Projections.Discover();
            await EventStore.Projections.Register();

            // Explicitly trigger replay to simulate SetDefinition() → observer.Replay() on definition change.
            // Register() updates the projection definition in the grain. We then manually call Replay()
            // to simulate the automatic replay that would happen if ReplayOnDefinitionChange was enabled.
            await EventStore.Projections.Replay<MaterializedProjection>();

            // Wait for the projection to reach the sequence number of the appended events,
            // which proves the replay completed with the new definition applied.
            await Projection.WaitTillReachesEventSequenceNumber(LastEventSequenceNumber);

            // Then confirm active state
            await Projection.WaitTillActive();

            ResultAfterReplay = await GetReadModel(EventSourceId);
        }
    }

    [Fact] void should_have_first_result_with_name_only() => Context.Result.Name.ShouldEqual("First Name");
    [Fact] void should_not_have_description_in_first_result() => Context.Result.Description.ShouldBeNull();
    [Fact] void should_have_name_in_replayed_result() => Context.ResultAfterReplay.Name.ShouldEqual("First Name");
    [Fact] void should_have_description_in_replayed_result() => Context.ResultAfterReplay.Description.ShouldEqual("First Description");
}
