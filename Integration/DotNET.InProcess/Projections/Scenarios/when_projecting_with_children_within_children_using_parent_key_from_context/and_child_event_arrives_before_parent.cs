// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context.Concepts;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context.ReadModels;
using Cratis.Chronicle.Observation;
using MongoDB.Driver;
using context = Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context.and_child_event_arrives_before_parent.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context;

[Collection(ChronicleCollection.Name)]
public class and_child_event_arrives_before_parent(context context) : Given<context>(context)
{
    const string SimulationName = "Test Simulation";
    const string ConfigurationName = "Test Configuration";
    const string HubName = "Test Hub";

    public class context(ChronicleInProcessFixture fixture) : Specification(fixture)
    {
        public SimulationId SimulationId;
        public ConfigurationId ConfigurationId;
        public HubId HubId;
        public IEnumerable<FailedPartition> FailedPartitions = [];
        public SimulationDashboard Result;
        public EventSequenceNumber LastEventSequenceNumber = EventSequenceNumber.First;

        public override IEnumerable<Type> EventTypes => [typeof(SimulationAdded), typeof(SimulationConfigurationAdded), typeof(HubAddedToSimulationConfiguration), typeof(WeightsSetForSimulationConfiguration)];
        public override IEnumerable<Type> Projections => [typeof(SimulationDashboardProjection)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(new SimulationDashboardProjection());
        }

        void Establish()
        {
            SimulationId = Guid.Parse("0089d57f-9095-4b56-b948-ab170de8e0ee");
            ConfigurationId = Guid.Parse("754fd741-adae-4fbd-8c47-2d622cc0b274");
            HubId = Guid.Parse("eff2cf7a-4121-438b-94fd-139775a09f57");
        }

        async Task Because()
        {
            var projection = EventStore.Projections.GetHandlerFor<SimulationDashboardProjection>();
            await projection.WaitTillActive();

            // Create root first
            var appendResult = await EventStore.EventLog.Append(SimulationId, new SimulationAdded(SimulationName));
            await projection.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            // OUT OF ORDER: Append Hub event BEFORE its Configuration parent
            // This should create a future since Configuration doesn't exist yet
            await EventStore.EventLog.Append(ConfigurationId, new HubAddedToSimulationConfiguration(HubId, HubName));

            // Now append the Configuration parent - this should resolve the Hub future
            appendResult = await EventStore.EventLog.Append(SimulationId, new SimulationConfigurationAdded(ConfigurationId, ConfigurationName));
            LastEventSequenceNumber = appendResult.SequenceNumber;

            // Wait for projection to catch up to the Configuration event and futures resolution
            await projection.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            FailedPartitions = await projection.GetFailedPartitions();

            var collection = ChronicleFixture.ReadModels.Database.GetCollection<SimulationDashboard>();
            var queryResult = await collection.FindAsync(_ => true);
            var allResults = await queryResult.ToListAsync();
            Result = allResults.FirstOrDefault();
        }
    }

    [Fact] void should_have_no_failed_partitions() => Context.FailedPartitions.ShouldBeEmpty();
    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_have_simulation_name() => Context.Result.Name.ShouldEqual(SimulationName);
    [Fact] void should_have_one_configuration() => Context.Result.Configurations.Count.ShouldEqual(1);
    [Fact] void should_have_configuration_id_on_child() => Context.Result.Configurations[0].ConfigurationId.ShouldEqual(Context.ConfigurationId);
    [Fact] void should_have_configuration_name_on_child() => Context.Result.Configurations[0].Name.ShouldEqual(ConfigurationName);
    [Fact] void should_have_one_hub_on_configuration() => Context.Result.Configurations[0].Hubs.Count.ShouldEqual(1);
    [Fact] void should_have_hub_id_on_nested_child() => Context.Result.Configurations[0].Hubs[0].HubId.ShouldEqual(Context.HubId);
    [Fact] void should_have_hub_name_on_nested_child() => Context.Result.Configurations[0].Hubs[0].Name.ShouldEqual(HubName);
    [Fact] void should_set_the_event_sequence_number_to_last_event() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
