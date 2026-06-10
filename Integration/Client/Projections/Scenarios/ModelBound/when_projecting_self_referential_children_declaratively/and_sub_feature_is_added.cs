// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_self_referential_children_declaratively.and_sub_feature_is_added.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_self_referential_children_declaratively;

// Isolates builder-vs-engine: the SAME self-referential shape as the model-bound scenarios, but defined
// with a hand-written DECLARATIVE projection that specifies explicit keys/parent-keys at every level.
// If the nested child gets its id + name here, the engine handles self-referential children correctly and
// the corruption is in the model-bound projection-definition builder. If it fails the same way, it's the engine.
[Collection(ChronicleCollection.Name)]
public class and_sub_feature_is_added(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public Guid ModuleId;
        public Guid RootFeatureId;
        public Guid SubFeatureId;
        public DeclModule Result;

        public override IEnumerable<Type> EventTypes =>
        [
            typeof(DeclModuleCreated),
            typeof(DeclFeatureAdded),
            typeof(DeclSubFeatureAdded),
            typeof(DeclFeatureRenamed)
        ];

        public override IEnumerable<Type> Projections => [typeof(DeclModuleProjection)];

        async Task Because()
        {
            ModuleId = Guid.Parse("cccccccc-0000-0000-0000-000000000001");
            RootFeatureId = Guid.Parse("cccccccc-0000-0000-0000-000000000002");
            SubFeatureId = Guid.Parse("cccccccc-0000-0000-0000-000000000003");

            var projectionId = EventStore.Projections.GetProjectionIdForModel<DeclModule>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillSubscribed();

            await EventStore.EventLog.Append(ModuleId, new DeclModuleCreated("My Module"));
            await EventStore.EventLog.Append(RootFeatureId, new DeclFeatureAdded(ModuleId, RootFeatureId, "Feature 1"));
            await EventStore.EventLog.Append(SubFeatureId, new DeclSubFeatureAdded(RootFeatureId, SubFeatureId, "Feature 2"));
            var appendResult = await EventStore.EventLog.Append(SubFeatureId, new DeclFeatureRenamed(SubFeatureId, "Renamed"));

            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            Result = await EventStore.ReadModels.GetInstanceById<DeclModule>(ModuleId.ToString());
        }
    }

    DeclFeature RootFeature => Context.Result.Features.Single(_ => _.Id == Context.RootFeatureId);

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_keep_only_the_root_feature_at_module_level() => Context.Result.Features.Count().ShouldEqual(1);
    [Fact] void should_set_the_top_level_feature_name() => RootFeature.Name.ShouldEqual("Feature 1");
    [Fact] void should_have_one_sub_feature_under_the_root() => RootFeature.SubFeatures.Count().ShouldEqual(1);
    [Fact] void should_give_the_nested_sub_feature_its_payload_id() => RootFeature.SubFeatures.Any(_ => _.Id == Context.SubFeatureId).ShouldBeTrue();

    // A keyed UPDATE event (the rename) resolves to the existing nested node via its creation event and lands
    // on the sub-feature at its actual depth.
    [Fact] void should_apply_the_rename_to_the_nested_sub_feature() => RootFeature.SubFeatures.Single().Name.ShouldEqual("Renamed");
}

[EventType]
public record DeclModuleCreated(string Name);

[EventType]
public record DeclFeatureAdded(Guid ModuleId, Guid FeatureId, string Name);

[EventType]
public record DeclSubFeatureAdded(Guid ParentFeatureId, Guid FeatureId, string Name);

[EventType]
public record DeclFeatureRenamed(Guid FeatureId, string Name);

public record DeclModule(Guid Id, string Name, IEnumerable<DeclFeature> Features);

public record DeclFeature(Guid Id, string Name, IEnumerable<DeclFeature> SubFeatures);

public class DeclModuleProjection : IProjectionFor<DeclModule>
{
    public void Define(IProjectionBuilderFor<DeclModule> builder) => builder
        .From<DeclModuleCreated>(b => b.Set(m => m.Name).To(e => e.Name))
        .Children(m => m.Features, features => features
            .IdentifiedBy(f => f.Id)
            .From<DeclFeatureAdded>(b => b
                .UsingParentKey(e => e.ModuleId)
                .UsingKey(e => e.FeatureId)
                .Set(f => f.Name).To(e => e.Name))
            .From<DeclFeatureRenamed>(b => b
                .UsingKey(e => e.FeatureId)
                .Set(f => f.Name).To(e => e.Name))
            .Children(f => f.SubFeatures, subFeatures => subFeatures
                .IdentifiedBy(sf => sf.Id)
                .From<DeclSubFeatureAdded>(b => b
                    .UsingParentKey(e => e.ParentFeatureId)
                    .UsingKey(e => e.FeatureId)
                    .Set(sf => sf.Name).To(e => e.Name))
                .From<DeclFeatureRenamed>(b => b
                    .UsingKey(e => e.FeatureId)
                    .Set(sf => sf.Name).To(e => e.Name))));
}

#pragma warning restore SA1402 // File may only contain a single type
