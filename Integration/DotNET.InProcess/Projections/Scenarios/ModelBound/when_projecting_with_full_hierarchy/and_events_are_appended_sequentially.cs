// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.ReadModels;
using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.when_projecting_with_full_hierarchy.and_events_are_appended_sequentially.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.when_projecting_with_full_hierarchy;

[Collection(ChronicleCollection.Name)]
public class and_events_are_appended_sequentially(context context) : Given<context>(context)
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
            ModuleId = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012");
            FeatureId = Guid.Parse("d4e5f6a7-b8c9-0123-defa-234567890123");
            SliceId = Guid.Parse("e5f6a7b8-c9d0-1234-efab-345678901234");
            EventItemId1 = Guid.Parse("f6a7b8c9-d0e1-2345-fabc-678901234567");
            EventItemId2 = Guid.Parse("a7b8c9d0-e1f2-3456-abcd-789012345678");

            var projectionId = EventStore.Projections.GetProjectionIdForModel<ModuleReadModel>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillActive();

            await EventStore.EventLog.Append(ModuleId, new ModuleAdded("Authors"));
            await EventStore.EventLog.Append(FeatureId, new FeatureAddedToModule(ModuleId, FeatureId, "Registration"));
            await EventStore.EventLog.Append(SliceId, new SliceAddedToFeature(FeatureId, SliceId, "Register Author"));
            await EventStore.EventLog.Append(SliceId, new EventAddedToSlice(SliceId, EventItemId1, "AuthorRegistered"));
            await EventStore.EventLog.Append(SliceId, new EventAddedToSlice(SliceId, EventItemId2, "AuthorUpdated"));
            var appendResult = await EventStore.EventLog.Append(SliceId, new CommandSetOnSlice("RegisterAuthor", "{}"));

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
