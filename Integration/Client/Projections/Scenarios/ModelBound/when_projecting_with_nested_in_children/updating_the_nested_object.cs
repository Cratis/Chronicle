// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.Events;
using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.ReadModels;
using MongoDB.Driver;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_with_nested_in_children.updating_the_nested_object.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_with_nested_in_children;

[Collection(ChronicleCollection.Name)]
public class updating_the_nested_object(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public Guid FeatureId;
        public Guid SliceId;
        public FeatureReadModel Result;

        public override IEnumerable<Type> EventTypes =>
        [
            typeof(FeatureCreated),
            typeof(SliceAddedToFeature),
            typeof(CommandSetOnSlice),
            typeof(CommandRenamedOnSlice),
            typeof(CommandClearedFromSlice),
            typeof(EventAddedToSlice)
        ];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(FeatureReadModel), typeof(SliceItem), typeof(SliceCommandItem)];

        async Task Because()
        {
            FeatureId = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012");
            SliceId = Guid.Parse("d4e5f6a7-b8c9-0123-def0-234567890123");

            var projectionId = EventStore.Projections.GetProjectionIdForModel<FeatureReadModel>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillActive();

            await EventStore.EventLog.Append(FeatureId, new FeatureCreated("My Feature"));
            await EventStore.EventLog.Append(SliceId, new SliceAddedToFeature(FeatureId, SliceId, "My Slice"));
            await EventStore.EventLog.Append(SliceId, new CommandSetOnSlice("Register", "{}"));
            var appendResult = await EventStore.EventLog.Append(SliceId, new CommandRenamedOnSlice("Create"));

            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            var collection = ChronicleFixture.ReadModels.Database.GetCollection<FeatureReadModel>();
            Result = await (await collection.FindAsync(_ => true)).FirstOrDefaultAsync();
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_have_one_slice() => Context.Result.Slices.Count().ShouldEqual(1);
    [Fact] void should_have_a_nested_command_on_the_slice() => Context.Result.Slices.First().Command.ShouldNotBeNull();
    [Fact] void should_update_the_command_name() => Context.Result.Slices.First().Command!.Name.ShouldEqual("Create");
    [Fact] void should_preserve_the_command_schema() => Context.Result.Slices.First().Command!.Schema.ShouldEqual("{}");
}
