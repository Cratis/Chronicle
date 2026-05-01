// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.when_projecting_with_deep_hierarchy_using_event_source_as_child_key.and_events_are_appended_sequentially.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.when_projecting_with_deep_hierarchy_using_event_source_as_child_key;

[Collection(ChronicleCollection.Name)]
public class and_events_are_appended_sequentially(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture fixture) : Specification(fixture)
    {
        public Guid ModuleId;
        public Guid FeatureId;
        public Guid SliceId;
        public Guid EventItemId;
        public DeepHierarchyModule Result;

        public override IEnumerable<Type> EventTypes =>
        [
            typeof(DeepHierarchyModuleCreated),
            typeof(DeepHierarchyFeatureCreated),
            typeof(DeepHierarchySliceCreated),
            typeof(DeepHierarchyEventCreated)
        ];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(DeepHierarchyModule)];

        async Task Because()
        {
            ModuleId = Guid.Parse("e5f6a7b8-c9d0-1234-efab-555555555555");
            FeatureId = Guid.Parse("f6a7b8c9-d0e1-2345-fabc-666666666666");
            SliceId = Guid.Parse("a7b8c9d0-e1f2-3456-abcd-777777777777");
            EventItemId = Guid.Parse("b8c9d0e1-f2a3-4567-bcde-888888888888");

            var projectionId = EventStore.Projections.GetProjectionIdForModel<DeepHierarchyModule>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillActive();

            await EventStore.EventLog.Append(ModuleId, new DeepHierarchyModuleCreated("Authors"));
            await EventStore.EventLog.Append(FeatureId, new DeepHierarchyFeatureCreated(ModuleId, FeatureId, "Registration"));
            await EventStore.EventLog.Append(SliceId, new DeepHierarchySliceCreated(FeatureId, SliceId, "Register Author"));
            var appendResult = await EventStore.EventLog.Append(SliceId, new DeepHierarchyEventCreated(SliceId, EventItemId, "AuthorRegistered"));

            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            var collection = ChronicleFixture.ReadModels.Database.GetCollection<DeepHierarchyModule>();
            Result = await (await collection.FindAsync(m => m.Id == ModuleId)).FirstOrDefaultAsync();
        }
    }

    DeepHierarchySlice Slice => Context.Result.Features.First().Slices.First();

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_set_the_module_name() => Context.Result.Name.ShouldEqual("Authors");
    [Fact] void should_have_one_feature() => Context.Result.Features.Count().ShouldEqual(1);
    [Fact] void should_set_the_feature_name() => Context.Result.Features.First().Name.ShouldEqual("Registration");
    [Fact] void should_have_one_slice_in_the_feature() => Context.Result.Features.First().Slices.Count().ShouldEqual(1);
    [Fact] void should_set_the_slice_name() => Slice.Name.ShouldEqual("Register Author");
    [Fact] void should_have_one_event_on_the_slice() => Slice.Events.Count().ShouldEqual(1);
    [Fact] void should_have_event_on_the_slice() => Slice.Events.Any(e => e.Id == Context.EventItemId && e.Name == "AuthorRegistered").ShouldBeTrue();
}
