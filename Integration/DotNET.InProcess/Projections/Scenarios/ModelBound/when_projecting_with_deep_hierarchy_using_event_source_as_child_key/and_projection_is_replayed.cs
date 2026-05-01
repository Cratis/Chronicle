// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.when_projecting_with_deep_hierarchy_using_event_source_as_child_key.and_projection_is_replayed.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ModelBound.when_projecting_with_deep_hierarchy_using_event_source_as_child_key;

/// <summary>
/// Integration spec that reproduces the bug where deeply nested children (4th level) lose
/// their data after a projection replay. The key distinction from other hierarchy tests is
/// that ONLY the root model is registered as a model-bound projection — intermediate types
/// are nested-only, not standalone projections. Events for the 4th-level child are appended
/// to the grandparent (Slice) event source, not to the child's own event source.
/// </summary>
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

        // Only the top-level type is registered — nested types are NOT standalone projections
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

            // Append to ModuleId event source
            await EventStore.EventLog.Append(ModuleId, new DeepHierarchyModuleCreated("Authors"));
            // Append to FeatureId event source (contains ModuleId as parentKey)
            await EventStore.EventLog.Append(FeatureId, new DeepHierarchyFeatureCreated(ModuleId, FeatureId, "Registration"));
            // Append to SliceId event source (contains FeatureId as parentKey; no explicit key → EventSourceId)
            await EventStore.EventLog.Append(SliceId, new DeepHierarchySliceCreated(FeatureId, SliceId, "Register Author"));
            // Append to SliceId event source (NOT EventItemId) — EventItemId is a property inside the event
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

/// <summary>
/// Event appended to a Module's event source to create a module.
/// </summary>
[EventType]
public record DeepHierarchyModuleCreated(string Name);

/// <summary>
/// Event appended to a Feature's own event source; carries ModuleId as parentKey.
/// </summary>
[EventType]
public record DeepHierarchyFeatureCreated(Guid ModuleId, Guid FeatureId, string Name);

/// <summary>
/// Event appended to a Slice's own event source (SliceId = event source); carries FeatureId as parentKey.
/// No explicit key on the ChildrenFrom → defaults to EventSourceId.
/// </summary>
[EventType]
public record DeepHierarchySliceCreated(Guid FeatureId, Guid SliceId, string Name);

/// <summary>
/// Event appended to the SLICE's event source (not the EventItemId event source).
/// The EventItemId is embedded as a property — this matches the Studio production pattern where
/// EventAddedToSlice is appended to the Slice event source with EventItemId as a field.
/// </summary>
[EventType]
public record DeepHierarchyEventCreated(Guid SliceId, Guid EventItemId, string Name);

/// <summary>
/// Root read model. Only this type is registered as a model-bound projection.
/// </summary>
[FromEvent<DeepHierarchyModuleCreated>]
public record DeepHierarchyModule(
    Guid Id,
    string Name,
    [ChildrenFrom<DeepHierarchyFeatureCreated>(
        key: nameof(DeepHierarchyFeatureCreated.FeatureId),
        parentKey: nameof(DeepHierarchyFeatureCreated.ModuleId),
        identifiedBy: nameof(DeepHierarchyFeature.Id))]
    IEnumerable<DeepHierarchyFeature> Features);

/// <summary>
/// Nested child — NOT registered as a standalone projection.
/// Uses EventSourceId as the child key (no explicit key on ChildrenFrom).
/// </summary>
[FromEvent<DeepHierarchyFeatureCreated>]
public record DeepHierarchyFeature(
    Guid Id,
    string Name,
    [ChildrenFrom<DeepHierarchySliceCreated>(
        parentKey: nameof(DeepHierarchySliceCreated.FeatureId),
        identifiedBy: nameof(DeepHierarchySlice.Id))]
    IEnumerable<DeepHierarchySlice> Slices);

/// <summary>
/// Grandchild — NOT registered as a standalone projection.
/// Events has key=EventItemId, parentKey auto-discovered (SliceId property matches Slice.Id type).
/// </summary>
[FromEvent<DeepHierarchySliceCreated>]
public record DeepHierarchySlice(
    Guid Id,
    string Name,
    [ChildrenFrom<DeepHierarchyEventCreated>(key: nameof(DeepHierarchyEventCreated.EventItemId))]
    IEnumerable<DeepHierarchyEvent> Events);

/// <summary>
/// Great-grandchild — NOT registered as a standalone projection.
/// Updated via class-level FromEvent; identified by EventItemId (auto-discovered via Id convention).
/// </summary>
[FromEvent<DeepHierarchyEventCreated>(key: nameof(DeepHierarchyEventCreated.EventItemId))]
public record DeepHierarchyEvent(Guid Id, string Name);

#pragma warning restore SA1402
