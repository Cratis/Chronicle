// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.when_projecting_child_with_class_level_parent_key.and_child_event_is_appended_to_its_own_event_source.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.when_projecting_child_with_class_level_parent_key;

[Collection(ChronicleCollection.Name)]
public class and_child_event_is_appended_to_its_own_event_source(context context) : Given<context>(context)
{
    const string DashboardName = "Operations";
    const string InitialConfigurationName = "North";
    const string UpdatedConfigurationName = "North Europe";

    public class context(ChronicleInProcessFixture fixture) : Specification(fixture)
    {
        public Guid DashboardId;
        public Guid ConfigurationId;
        public DashboardReadModel Result;

        public override IEnumerable<Type> EventTypes => [typeof(ModelBoundDashboardAdded), typeof(ModelBoundConfigurationAddedToDashboard), typeof(ModelBoundConfigurationRenamed)];
        public override IEnumerable<Type> ModelBoundProjections => [typeof(DashboardReadModel), typeof(ConfigurationReadModel)];

        async Task Because()
        {
            DashboardId = Guid.Parse("d75f7e3a-635a-4208-9d66-798ff0ee0d69");
            ConfigurationId = Guid.Parse("7c9cf690-0133-4e12-abba-43187220bb2a");

            var projectionId = EventStore.Projections.GetProjectionIdForModel<DashboardReadModel>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillActive();

            await EventStore.EventLog.Append(DashboardId, new ModelBoundDashboardAdded(DashboardName));
            await EventStore.EventLog.Append(ConfigurationId, new ModelBoundConfigurationAddedToDashboard(DashboardId, ConfigurationId, InitialConfigurationName));
            var appendResult = await EventStore.EventLog.Append(ConfigurationId, new ModelBoundConfigurationRenamed(DashboardId, UpdatedConfigurationName));

            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            var collection = ChronicleFixture.ReadModels.Database.GetCollection<DashboardReadModel>();
            Result = await (await collection.FindAsync(_ => true)).FirstOrDefaultAsync();
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_keep_the_dashboard() => Context.Result.Name.ShouldEqual(DashboardName);
    [Fact] void should_have_one_configuration() => Context.Result.Configurations.Count().ShouldEqual(1);
    [Fact] void should_keep_the_child_identifier() => Context.Result.Configurations.First().Id.ShouldEqual(Context.ConfigurationId);
    [Fact] void should_update_the_child_from_the_separate_event_source() => Context.Result.Configurations.First().Name.ShouldEqual(UpdatedConfigurationName);
}

[EventType]
public record ModelBoundDashboardAdded(string Name);

[EventType]
public record ModelBoundConfigurationAddedToDashboard(Guid DashboardId, Guid ConfigurationId, string Name);

[EventType]
public record ModelBoundConfigurationRenamed(Guid DashboardId, string Name);

[FromEvent<ModelBoundConfigurationRenamed>(parentKey: nameof(ModelBoundConfigurationRenamed.DashboardId))]
public record ConfigurationReadModel(
    Guid Id,
    string Name);

[FromEvent<ModelBoundDashboardAdded>]
public record DashboardReadModel(
    Guid Id,
    string Name,
    [ChildrenFrom<ModelBoundConfigurationAddedToDashboard>(
        key: nameof(ModelBoundConfigurationAddedToDashboard.ConfigurationId),
        identifiedBy: nameof(ConfigurationReadModel.Id),
        parentKey: nameof(ModelBoundConfigurationAddedToDashboard.DashboardId))]
    IEnumerable<ConfigurationReadModel> Configurations);

#pragma warning restore SA1402 // File may only contain a single type
