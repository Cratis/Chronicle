// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Registrations;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_updating_projection_definition.for_immediate_projection.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_updating_projection_definition;

[Collection(ChronicleCollection.Name)]
public class for_immediate_projection(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification(chronicleFixture)
    {
        public ImmediateReadModel Result;
        public ImmediateReadModel ResultAfterUpdate;

        public override IEnumerable<Type> EventTypes => [typeof(TestEvent)];
        public override IEnumerable<Type> Projections => [typeof(ImmediateProjection)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            ImmediateProjection.MapBothProperties = false;
            services.AddSingleton(new ImmediateProjection());
        }

        async Task Because()
        {
            // An immediate projection has no observer to wait for; the read model is computed on demand from the events.
            var eventSourceId = Guid.NewGuid().ToString();
            await EventStore.EventLog.Append(eventSourceId, new TestEvent("First Name", "First Description"));
            Result = await EventStore.ReadModels.GetInstanceById<ImmediateReadModel>(eventSourceId);

            // The client's own registration has to be finished, otherwise it can land after the manual one below with the old definition.
            var outcome = await EventStore.WaitForRegistration(TimeSpanFactory.FromSeconds(30));
            Assert.True(outcome.IsSuccess, $"Client registration did not succeed: {outcome.Failure}");

            ImmediateProjection.MapBothProperties = true;
            await EventStore.Projections.Discover();
            await EventStore.Projections.Register();

            await EventStore.EventLog.Append(eventSourceId, new TestEvent("Second Name", "Second Description"));
            ResultAfterUpdate = await EventStore.ReadModels.GetInstanceById<ImmediateReadModel>(eventSourceId);
        }
    }

    [Fact] void should_have_first_result_with_name_only() => Context.Result.Name.ShouldEqual("First Name");
    [Fact] void should_not_have_description_in_first_result() => Context.Result.Description.ShouldBeNull();
    [Fact] void should_have_updated_result_with_name() => Context.ResultAfterUpdate.Name.ShouldEqual("Second Name");
    [Fact] void should_have_description_in_updated_result() => Context.ResultAfterUpdate.Description.ShouldEqual("Second Description");
}
