// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;
using context =Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_renaming_a_nested_self_referential_feature.and_rename_carries_module_as_parent_key.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_renaming_a_nested_self_referential_feature;

// Model-bound mirror of Cratis Studio's Module/Feature read model (self-referential Feature with SubFeatures).
// A sub-feature is added, then renamed. The sub-feature must stay nested under its parent (not promoted to a
// second top-level feature) and receive the new name.
//
// This exercised two issues, both now fixed:
//   1) BUILDER: the model-bound builder leaked an ancestor collection's creator (FeatureAdded) into the
//      self-referential SubFeatures definition, materializing the feature as a child of itself.
//      ChildrenDefinitionExtensions now excludes ancestor creators from self-referential propagation.
//   2) ENGINE: a keyed UPDATE event (FeatureRenamed) upserted a phantom top-level feature (promotion) and never
//      reached the real nested sub-feature. The engine now resolves a keyed event to the existing node via its
//      creation event at whatever depth it lives, and a projection level skips a key that targets a deeper
//      collection. The rename lands on the nested sub-feature, with no promotion and no parent rename.
[Collection(ChronicleCollection.Name)]
public class and_rename_carries_module_as_parent_key(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public Guid ModuleId;
        public Guid RootFeatureId;
        public Guid SubFeatureId;
        public MbModule Result;

        public override IEnumerable<Type> EventTypes =>
        [
            typeof(MbModuleAdded),
            typeof(MbFeatureAdded),
            typeof(MbSubFeatureAdded),
            typeof(MbFeatureRenamed)
        ];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(MbModule), typeof(MbFeature)];

        async Task Because()
        {
            ModuleId = Guid.Parse("dddddddd-0000-0000-0000-000000000001");
            RootFeatureId = Guid.Parse("dddddddd-0000-0000-0000-000000000002");
            SubFeatureId = Guid.Parse("dddddddd-0000-0000-0000-000000000003");

            var projectionId = EventStore.Projections.GetProjectionIdForModel<MbModule>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillSubscribed();

            await EventStore.EventLog.Append(ModuleId, new MbModuleAdded(ModuleId, "My Module"));
            await EventStore.EventLog.Append(RootFeatureId, new MbFeatureAdded(ModuleId, RootFeatureId, "Feature 1"));
            await EventStore.EventLog.Append(SubFeatureId, new MbSubFeatureAdded(ModuleId, RootFeatureId, "Feature 2"));
            var appendResult = await EventStore.EventLog.Append(SubFeatureId, new MbFeatureRenamed(ModuleId, SubFeatureId, "Renamed"));

            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            Result = await EventStore.ReadModels.GetInstanceById<MbModule>(ModuleId.ToString());
        }
    }

    MbFeature RootFeature => Context.Result.Features.Single(_ => _.Id == Context.RootFeatureId);

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_keep_only_the_root_feature_at_module_level() => Context.Result.Features.Count().ShouldEqual(1);
    [Fact] void should_not_promote_the_sub_feature() => Context.Result.Features.Any(_ => _.Id == Context.SubFeatureId).ShouldBeFalse();
    [Fact] void should_have_one_sub_feature_under_the_root() => RootFeature.SubFeatures.Count().ShouldEqual(1);
    [Fact] void should_identify_the_nested_sub_feature() => RootFeature.SubFeatures.Any(_ => _.Id == Context.SubFeatureId).ShouldBeTrue();
    [Fact] void should_not_make_the_root_feature_a_child_of_itself() => RootFeature.SubFeatures.Any(_ => _.Id == Context.RootFeatureId).ShouldBeFalse();
    [Fact] void should_not_rename_the_parent_feature() => RootFeature.Name.ShouldEqual("Feature 1");
    [Fact] void should_apply_the_rename_to_the_nested_sub_feature() => RootFeature.SubFeatures.Any(_ => _.Id == Context.SubFeatureId && _.Name == "Renamed").ShouldBeTrue();
}

[EventType]
public record MbModuleAdded(Guid ModuleId, string Name);

[EventType]
public record MbFeatureAdded(Guid ModuleId, Guid FeatureId, string Name);

[EventType]
public record MbSubFeatureAdded(Guid ModuleId, Guid ParentFeatureId, string Name);

[EventType]
public record MbFeatureRenamed(Guid ModuleId, Guid FeatureId, string NewName);

[FromEvent<MbFeatureAdded>]
[FromEvent<MbSubFeatureAdded>(parentKey: nameof(MbSubFeatureAdded.ParentFeatureId))]
[FromEvent<MbFeatureRenamed>(key: nameof(MbFeatureRenamed.FeatureId), parentKey: nameof(MbFeatureRenamed.ModuleId))]
public record MbFeature(
    [Key] Guid Id,
    [SetFrom<MbFeatureRenamed>(nameof(MbFeatureRenamed.NewName))]
    string Name,
    [ChildrenFrom<MbSubFeatureAdded>(
        parentKey: nameof(MbSubFeatureAdded.ParentFeatureId),
        identifiedBy: nameof(MbFeature.Id))]
    IEnumerable<MbFeature> SubFeatures);

[FromEvent<MbModuleAdded>]
public record MbModule(
    [Key] Guid Id,
    string Name,
    [ChildrenFrom<MbFeatureAdded>(
        key: nameof(MbFeatureAdded.FeatureId),
        parentKey: nameof(MbFeatureAdded.ModuleId),
        identifiedBy: nameof(MbFeature.Id))]
    IEnumerable<MbFeature> Features);

#pragma warning restore SA1402 // File may only contain a single type
