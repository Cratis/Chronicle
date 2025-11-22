// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_updating_projection_definition.and_projection_is_updated.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_updating_projection_definition;

[Collection(ChronicleCollection.Name)]
public class and_projection_is_updated(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.a_projection_and_events_appended_to_it<InitialProjection, TestReadModel>(chronicleInProcessFixture)
    {
        public TestReadModel ResultAfterUpdate;
        public TestEvent SecondEvent;
        public InitialProjection ProjectionInstance;

        public override IEnumerable<Type> EventTypes => [typeof(TestEvent)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            InitialProjection.MapBothProperties = false;
            ProjectionInstance = new InitialProjection();
            services.AddSingleton(ProjectionInstance);
        }

        void Establish()
        {
            EventsToAppend.Add(new TestEvent("First Name", "First Description"));
            SecondEvent = new TestEvent("Second Name", "Second Description");
        }

        async Task Because()
        {
            // Update the projection to map both properties
            InitialProjection.MapBothProperties = true;

            // Re-register the projection
            await EventStore.Projections.Discover();
            await EventStore.Projections.Register();

            // Wait for projection to become active
            await Projection.WaitTillActive();

            // Append second event
            var appendResult = await EventStore.EventLog.Append(EventSourceId, SecondEvent);

            // Wait for it to be processed
            await Projection.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            // Get the updated result
            ResultAfterUpdate = await GetReadModel(EventSourceId);
        }
    }

    [Fact] void should_have_first_result_with_name_only() => Context.Result.Name.ShouldEqual("First Name");
    [Fact] void should_not_have_description_in_first_result() => Context.Result.Description.ShouldBeNull();
    [Fact] void should_have_updated_result_with_name() => Context.ResultAfterUpdate.Name.ShouldEqual("Second Name");
    [Fact] void should_have_description_in_updated_result() => Context.ResultAfterUpdate.Description.ShouldEqual("Second Description");
}


