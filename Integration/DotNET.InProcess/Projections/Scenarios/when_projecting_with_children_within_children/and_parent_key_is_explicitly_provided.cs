// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children.Concepts;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children.ReadModels;
using Cratis.Chronicle.Observation;
using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children.and_parent_key_is_explicitly_provided.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children;

[Collection(ChronicleCollection.Name)]
public class and_parent_key_is_explicitly_provided(context context) : Given<context>(context)
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
        public Simulation Result;
        public EventSequenceNumber LastEventSequenceNumber = EventSequenceNumber.First;

        public override IEnumerable<Type> EventTypes => [typeof(SimulationAdded), typeof(ConfigurationAddedToSimulation), typeof(HubAddedToConfiguration)];
        public override IEnumerable<Type> Projections => [typeof(SimulationProjection)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(new SimulationProjection());
        }

        void Establish()
        {
            SimulationId = SimulationId.New();
            ConfigurationId = ConfigurationId.New();
            HubId = HubId.New();
        }

        async Task Because()
        {
            var projection = EventStore.Projections.GetHandlerFor<SimulationProjection>();
            await projection.WaitTillActive();

            // Append root event to SimulationId event source
            var appendResult = await EventStore.EventLog.Append(SimulationId, new SimulationAdded(SimulationName));
            LastEventSequenceNumber = appendResult.SequenceNumber;

            // Append first level child to ConfigurationId event source with SimulationId in content
            appendResult = await EventStore.EventLog.Append(ConfigurationId, new ConfigurationAddedToSimulation(SimulationId, ConfigurationId, ConfigurationName));
            LastEventSequenceNumber = appendResult.SequenceNumber;

            // Append second level child to HubId event source with ConfigurationId in content
            appendResult = await EventStore.EventLog.Append(HubId, new HubAddedToConfiguration(ConfigurationId, HubId, HubName));
            LastEventSequenceNumber = appendResult.SequenceNumber;

            // Wait for projection to process all events
            await projection.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            // Get failures if any
            FailedPartitions = await projection.GetFailedPartitions();

            // Get the result
            var collection = ChronicleFixture.ReadModels.Database.GetCollection<Simulation>();
            var queryResult = await collection.FindAsync(_ => true);
            Result = queryResult.FirstOrDefault();
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
