// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context.Concepts;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context.ReadModels;
using Cratis.Chronicle.Observation;
using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context.and_all_events_are_appended_in_one_transaction.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context;

[Collection(ChronicleCollection.Name)]
public class and_all_events_are_appended_in_one_transaction(context context) : Given<context>(context)
{
    const string SimulationName = "Test Simulation";
    const string ConfigurationName = "Test Configuration";
    const string HubName = "Test Hub";

    const double Distance = 100.0;
    const double Time = 2.5;
    const double Cost = 50.0;
    const double Waste = 10.0;

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

            var events = new EventForEventSourceId[]
            {
                new(ConfigurationId, new WeightsSetForSimulationConfiguration(Distance, Time, Cost, Waste), Causation.Unknown()),
                new(ConfigurationId, new HubAddedToSimulationConfiguration(HubId, HubName), Causation.Unknown()),
                new(SimulationId, new SimulationAdded(SimulationName), Causation.Unknown()),
                new(SimulationId, new SimulationConfigurationAdded(ConfigurationId, ConfigurationName), Causation.Unknown())
            };

            var appendResult = await EventStore.EventLog.AppendMany(events);
            LastEventSequenceNumber = appendResult.SequenceNumbers.Last();

            await projection.WaitTillReachesEventSequenceNumber(LastEventSequenceNumber);

            FailedPartitions = await projection.GetFailedPartitions();

            var collection = ChronicleFixture.ReadModels.Database.GetCollection<SimulationDashboard>();
            var queryResult = await collection.FindAsync(_ => true);
            var allResults = await queryResult.ToListAsync();
            Result = allResults.FirstOrDefault();
        }
    }

    [Fact]
    void should_have_no_failed_partitions()
    {
        if (Context.FailedPartitions.Any())
        {
            var failures = Context.FailedPartitions.ToList();
            var messages = failures.SelectMany(f => f.Attempts.Select(a => a.Messages)).SelectMany(m => m).ToList();
            var stackTraces = failures.SelectMany(f => f.Attempts.Select(a => a.StackTrace)).ToList();
            var combined = string.Join("\n\n", messages.Zip(stackTraces, (msg, stack) => $"Message: {msg}\nStack: {stack}"));
            throw new Xunit.Sdk.XunitException($"Failed partitions:\n{combined}");
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
    [Fact] void should_set_distance_on_configuration() => Context.Result.Configurations[0].Distance.ShouldEqual(Distance);
    [Fact] void should_set_time_on_configuration() => Context.Result.Configurations[0].Time.ShouldEqual(Time);
    [Fact] void should_set_cost_on_configuration() => Context.Result.Configurations[0].Cost.ShouldEqual(Cost);
    [Fact] void should_set_waste_on_configuration() => Context.Result.Configurations[0].Waste.ShouldEqual(Waste);
}
