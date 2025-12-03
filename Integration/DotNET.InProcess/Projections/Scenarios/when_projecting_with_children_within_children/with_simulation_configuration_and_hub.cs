// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children.Concepts;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children.ReadModels;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children.with_simulation_configuration_and_hub.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children;

[Collection(ChronicleCollection.Name)]
public class with_simulation_configuration_and_hub(context context) : Given<context>(context)
{
    const string SimulationName = "Test Simulation";
    const string ConfigurationName = "Test Configuration";
    const string HubName = "Test Hub";

    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.a_projection_and_events_appended_to_it<SimulationProjection, Simulation>(chronicleInProcessFixture)
    {
        public SimulationId SimulationId;
        public ConfigurationId ConfigurationId;
        public HubId HubId;

        public override IEnumerable<Type> EventTypes => [typeof(SimulationAdded), typeof(ConfigurationAddedToSimulation), typeof(HubAddedToConfiguration)];

        void Establish()
        {
            SimulationId = SimulationId.New();
            ConfigurationId = ConfigurationId.New();
            HubId = HubId.New();

            EventSourceId = SimulationId;
            ReadModelId = SimulationId.Value.ToString();

            EventsWithEventSourceIdToAppend.Add(new(SimulationId, new SimulationAdded(SimulationName)));
            EventsWithEventSourceIdToAppend.Add(new(SimulationId, new ConfigurationAddedToSimulation(ConfigurationId, ConfigurationName)));
            EventsWithEventSourceIdToAppend.Add(new(ConfigurationId, new HubAddedToConfiguration(HubId, HubName)));
        }
    }

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
