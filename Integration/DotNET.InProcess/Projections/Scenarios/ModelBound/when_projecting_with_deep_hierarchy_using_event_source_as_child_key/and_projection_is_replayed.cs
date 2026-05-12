// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Projections.ModelBound;
using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.when_projecting_with_deep_hierarchy_using_event_source_as_child_key.and_projection_is_replayed.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.when_projecting_with_deep_hierarchy_using_event_source_as_child_key;

[Collection(ChronicleCollection.Name)]
public class and_projection_is_replayed(context context) : Given<context>(context)
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
            ModuleId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-111111111111");
            FeatureId = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-222222222222");
            SliceId = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-333333333333");
            EventItemId = Guid.Parse("d4e5f6a7-b8c9-0123-defa-444444444444");

            var projectionId = EventStore.Projections.GetProjectionIdForModel<DeepHierarchyModule>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillActive();

            await EventStore.EventLog.Append(ModuleId, new DeepHierarchyModuleCreated("Authors"));
            await EventStore.EventLog.Append(FeatureId, new DeepHierarchyFeatureCreated(ModuleId, FeatureId, "Registration"));
            await EventStore.EventLog.Append(SliceId, new DeepHierarchySliceCreated(FeatureId, SliceId, "Register Author"));
            var appendResult = await EventStore.EventLog.Append(SliceId, new DeepHierarchyEventCreated(SliceId, EventItemId, "AuthorRegistered"));

            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            await EventStore.Projections.Replay(projectionId);
            await EventStore.Jobs.WaitForThereToBeNoJobs();
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

[EventType]
public record DeepHierarchyModuleCreated(string Name);

[EventType]
public record DeepHierarchyFeatureCreated(Guid ModuleId, Guid FeatureId, string Name);

[EventType]
public record DeepHierarchySliceCreated(Guid FeatureId, Guid SliceId, string Name);

[EventType]
public record DeepHierarchyEventCreated(Guid SliceId, Guid EventItemId, string Name);

[FromEvent<DeepHierarchyModuleCreated>]
public record DeepHierarchyModule(
    Guid Id,
    string Name,
    [ChildrenFrom<DeepHierarchyFeatureCreated>(
        key: nameof(DeepHierarchyFeatureCreated.FeatureId),
        parentKey: nameof(DeepHierarchyFeatureCreated.ModuleId),
        identifiedBy: nameof(DeepHierarchyFeature.Id))]
    IEnumerable<DeepHierarchyFeature> Features);

[FromEvent<DeepHierarchyFeatureCreated>]
public record DeepHierarchyFeature(
    Guid Id,
    string Name,
    [ChildrenFrom<DeepHierarchySliceCreated>(
        parentKey: nameof(DeepHierarchySliceCreated.FeatureId),
        identifiedBy: nameof(DeepHierarchySlice.Id))]
    IEnumerable<DeepHierarchySlice> Slices);

[FromEvent<DeepHierarchySliceCreated>]
public record DeepHierarchySlice(
    Guid Id,
    string Name,
    [ChildrenFrom<DeepHierarchyEventCreated>(key: nameof(DeepHierarchyEventCreated.EventItemId))]
    IEnumerable<DeepHierarchyEvent> Events);

[FromEvent<DeepHierarchyEventCreated>(key: nameof(DeepHierarchyEventCreated.EventItemId))]
public record DeepHierarchyEvent(Guid Id, string Name);

#pragma warning restore SA1402
