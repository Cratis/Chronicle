// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Specifications.Projections.Events;
using Cratis.Chronicle.Integration.Specifications.Projections.ProjectionTypes;
using Cratis.Chronicle.Observation;
using context = Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_registering_multiple_projections.all_become_active.context;

namespace Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_registering_multiple_projections;

[Collection(ChronicleCollection.Name)]
public class all_become_active(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public IEnumerable<IProjectionHandler> Handlers;
        public ProjectionState[] States;

        public override IEnumerable<Type> Projections =>
        [
            typeof(AutoMappedPropertiesProjection),
            typeof(IncrementingProjection),
            typeof(DecrementingProjection),
            typeof(SetPropertiesProjection),
            typeof(SetValuesProjection)
        ];

        public override IEnumerable<Type> EventTypes =>
        [
            typeof(EventWithPropertiesForAllSupportedTypes),
            typeof(EmptyEvent)
        ];

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(new AutoMappedPropertiesProjection());
            services.AddSingleton(new IncrementingProjection());
            services.AddSingleton(new DecrementingProjection());
            services.AddSingleton(new SetPropertiesProjection());
            services.AddSingleton(new SetValuesProjection());
        }

        async Task Because()
        {
            Handlers = EventStore.Projections.GetAllHandlers().ToArray();
            await Task.WhenAll(Handlers.Select(h => h.WaitTillActive()));
            States = await Task.WhenAll(Handlers.Select(h => h.GetState()));
        }
    }

    [Fact] void should_register_all_projections() => Context.Handlers.Count().ShouldEqual(5);
    [Fact] void should_have_all_projections_active() => Context.States.All(s => s.RunningState == ObserverRunningState.Active).ShouldBeTrue();
}
