// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context.Concepts;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context.ReadModels;
using Cratis.Chronicle.Observation;
using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context.and_metric_event_arrives_before_hub_is_added.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children_within_children_using_parent_key_from_context;

[Collection(ChronicleCollection.Name)]
public class and_metric_event_arrives_before_hub_is_added(context context) : Given<context>(context)
{
    const string SimulationName = "Test Simulation";
    const string ConfigurationName = "Test Configuration";
    const string HubName = "Test Hub";
    const string MetricName = "Test Metric";

    public class context(ChronicleInProcessFixture fixture) : Specification(fixture)
    {
        public SimulationId SimulationId;
        public ConfigurationId ConfigurationId;
        public HubId HubId;
        public MetricId MetricId;
        public IEnumerable<FailedPartition> FailedPartitions = [];
        public SimulationDashboard Result;
        public EventSequenceNumber LastEventSequenceNumber = EventSequenceNumber.First;

        public override IEnumerable<Type> EventTypes =>
        [
            typeof(SimulationAdded),
            typeof(SimulationConfigurationAdded),
            typeof(HubAddedToSimulationConfiguration),
            typeof(WeightsSetForSimulationConfiguration),
            typeof(MetricAddedToHub)
        ];

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
            MetricId = Guid.Parse("de8ebf14-2423-47b3-8b0f-dc6f4f65b74a");
        }

        async Task Because()
        {
            var projection = EventStore.Projections.GetHandlerFor<SimulationDashboardProjection>();
            await projection.WaitTillActive();

            var appendResult = await EventStore.EventLog.Append(SimulationId, new SimulationAdded(SimulationName));
            await projection.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            appendResult = await EventStore.EventLog.Append(SimulationId, new SimulationConfigurationAdded(ConfigurationId, ConfigurationName));
            await projection.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            // Third nested child arrives before its Hub parent exists.
            await EventStore.EventLog.Append(HubId, new MetricAddedToHub(MetricId, MetricName));

            appendResult = await EventStore.EventLog.Append(ConfigurationId, new HubAddedToSimulationConfiguration(HubId, HubName));
            LastEventSequenceNumber = appendResult.SequenceNumber;
            await projection.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            FailedPartitions = await projection.GetFailedPartitions();

            var collection = ChronicleFixture.ReadModels.Database.GetCollection<SimulationDashboard>();
            var queryResult = await collection.FindAsync(_ => true);
            Result = (await queryResult.ToListAsync()).FirstOrDefault();
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
    [Fact] void should_have_configuration_name_on_child() => Context.Result.Configurations[0].Name.ShouldEqual(ConfigurationName);
    [Fact] void should_have_one_hub_on_configuration() => Context.Result.Configurations[0].Hubs.Count.ShouldEqual(1);
    [Fact] void should_have_hub_name_on_nested_child() => Context.Result.Configurations[0].Hubs[0].Name.ShouldEqual(HubName);
    [Fact] void should_have_one_metric_on_hub() => Context.Result.Configurations[0].Hubs[0].Metrics.Count.ShouldEqual(1);
    [Fact] void should_set_the_event_sequence_number_to_last_event() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
