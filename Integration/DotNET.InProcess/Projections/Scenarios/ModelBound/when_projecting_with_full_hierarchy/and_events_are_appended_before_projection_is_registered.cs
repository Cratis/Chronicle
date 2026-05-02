// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.ReadModels;
using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.when_projecting_with_full_hierarchy.and_events_are_appended_before_projection_is_registered.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.when_projecting_with_full_hierarchy;

[Collection(ChronicleCollection.Name)]
public class and_events_are_appended_before_projection_is_registered(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : Specification(fixture)
    {
        public Guid ModuleId;
        public Guid FeatureId;
        public Guid SliceId;
        public Guid EventItemId1;
        public Guid EventItemId2;
        public ModuleReadModel Result;

        public override IEnumerable<Type> EventTypes =>
        [
            typeof(ModuleAdded),
            typeof(FeatureAddedToModule),
            typeof(SliceAddedToFeature),
            typeof(EventAddedToSlice),
            typeof(CommandSetOnSlice),
            typeof(CommandRenamedOnSlice),
            typeof(CommandClearedFromSlice)
        ];

        public override IEnumerable<Type> ModelBoundProjections =>
            [typeof(ModuleReadModel), typeof(FeatureItem), typeof(SliceItem), typeof(SliceCommandItem), typeof(EventItem)];

        async Task Because()
        {
            ModuleId = Guid.Parse("f6a7b8c9-d0e1-2345-fab0-456789012345");
            FeatureId = Guid.Parse("a7b8c9d0-e1f2-3456-0abc-567890123456");
            SliceId = Guid.Parse("b8c9d0e1-f2a3-4567-1bcd-678901234567");
            EventItemId1 = Guid.Parse("c9d0e1f2-a3b4-5678-2cde-789012345678");
            EventItemId2 = Guid.Parse("d0e1f2a3-b4c5-6789-3def-890123456789");

            await EventStore.EventLog.Append(ModuleId, new ModuleAdded("Authors"));
            await EventStore.EventLog.Append(FeatureId, new FeatureAddedToModule(ModuleId, FeatureId, "Registration"));
            await EventStore.EventLog.Append(SliceId, new SliceAddedToFeature(FeatureId, SliceId, "Register Author"));
            await EventStore.EventLog.Append(SliceId, new EventAddedToSlice(SliceId, EventItemId1, "AuthorRegistered"));
            await EventStore.EventLog.Append(SliceId, new EventAddedToSlice(SliceId, EventItemId2, "AuthorUpdated"));
            var appendResult = await EventStore.EventLog.Append(SliceId, new CommandSetOnSlice("RegisterAuthor", "{}"));

            var projectionId = EventStore.Projections.GetProjectionIdForModel<ModuleReadModel>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillActive();
            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            var collection = ChronicleFixture.ReadModels.Database.GetCollection<ModuleReadModel>();
            Result = await (await collection.FindAsync(m => m.Id == ModuleId)).FirstOrDefaultAsync();
        }
    }

    SliceItem Slice => Context.Result.Features.First().Slices.First();

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_set_the_module_name() => Context.Result.Name.ShouldEqual("Authors");
    [Fact] void should_have_one_feature() => Context.Result.Features.Count().ShouldEqual(1);
    [Fact] void should_set_the_feature_name() => Context.Result.Features.First().Name.ShouldEqual("Registration");
    [Fact] void should_have_one_slice_in_the_feature() => Context.Result.Features.First().Slices.Count().ShouldEqual(1);
    [Fact] void should_set_the_slice_name() => Slice.Name.ShouldEqual("Register Author");
    [Fact] void should_have_two_events_on_the_slice() => Slice.Events.Count().ShouldEqual(2);
    [Fact] void should_have_first_event_on_the_slice() => Slice.Events.Any(e => e.Id == Context.EventItemId1 && e.Name == "AuthorRegistered").ShouldBeTrue();
    [Fact] void should_have_second_event_on_the_slice() => Slice.Events.Any(e => e.Id == Context.EventItemId2 && e.Name == "AuthorUpdated").ShouldBeTrue();
    [Fact] void should_have_a_command_on_the_slice() => Slice.Command.ShouldNotBeNull();
    [Fact] void should_set_the_command_name() => Slice.Command.Name.ShouldEqual("RegisterAuthor");
}
