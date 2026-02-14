// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using given = Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.given;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_updating_projection_definition.for_materialized_projection.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_updating_projection_definition;

[Collection(ChronicleCollection.Name)]
public class for_materialized_projection(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.a_projection_and_events_appended_to_it<MaterializedProjection, TestReadModel>(chronicleInProcessFixture)
    {
        public TestReadModel ResultAfterUpdate;
        public TestEvent SecondEvent;

        public override IEnumerable<Type> EventTypes => [typeof(TestEvent)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            MaterializedProjection.MapBothProperties = false;
            services.AddSingleton(new MaterializedProjection());
        }

        void Establish()
        {
            EventsToAppend.Add(new TestEvent("First Name", "First Description"));
            SecondEvent = new TestEvent("Second Name", "Second Description");
        }

        async Task Because()
        {
            MaterializedProjection.MapBothProperties = true;

            await EventStore.Projections.Discover();
            await EventStore.Projections.Register();

            await Projection.WaitTillActive();

            var appendResult = await EventStore.EventLog.Append(EventSourceId, SecondEvent);

            await Projection.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            ResultAfterUpdate = await GetReadModel(EventSourceId);
        }
    }

    [Fact] void should_have_first_result_with_name_only() => Context.Result.Name.ShouldEqual("First Name");
    [Fact] void should_not_have_description_in_first_result() => Context.Result.Description.ShouldBeNull();
    [Fact] void should_have_updated_result_with_name() => Context.ResultAfterUpdate.Name.ShouldEqual("Second Name");
    [Fact] void should_have_description_in_updated_result() => Context.ResultAfterUpdate.Description.ShouldEqual("Second Description");
}


