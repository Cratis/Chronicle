// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_having;

public class self_referencing_children_with_keyed_from_event : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(SelfRefKeyedModuleCreated),
            typeof(SelfRefKeyedFeatureAdded),
            typeof(SelfRefKeyedSubFeatureAdded),
            typeof(SelfRefKeyedFeatureUITemplateSet)
        ]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(SelfRefKeyedModule));

    [Fact]
    void should_not_add_keyed_from_event_as_nested_sub_feature_creator()
    {
        var eventType = event_types.GetEventTypeFor(typeof(SelfRefKeyedFeatureUITemplateSet)).ToContract();
        var featureChildrenDef = _result.Children[nameof(SelfRefKeyedModule.Features)];
        var subFeaturesDef = featureChildrenDef.Children[nameof(SelfRefKeyedFeature.SubFeatures)];
        subFeaturesDef.From.Keys.ShouldNotContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_still_add_structural_self_ref_event_to_nested_sub_features()
    {
        var eventType = event_types.GetEventTypeFor(typeof(SelfRefKeyedSubFeatureAdded)).ToContract();
        var featureChildrenDef = _result.Children[nameof(SelfRefKeyedModule.Features)];
        var subFeaturesDef = featureChildrenDef.Children[nameof(SelfRefKeyedFeature.SubFeatures)];
        subFeaturesDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }
}

[EventType]
public record SelfRefKeyedModuleCreated(string Name);

[EventType]
public record SelfRefKeyedFeatureAdded(SelfRefKeyedModuleId ModuleId, SelfRefKeyedFeatureId FeatureId, string Name);

[EventType]
public record SelfRefKeyedSubFeatureAdded(SelfRefKeyedFeatureId ParentFeatureId, string Name);

[EventType]
public record SelfRefKeyedFeatureUITemplateSet(SelfRefKeyedFeatureId FeatureId, string UITemplateId);

public record SelfRefKeyedModuleId(Guid Value);
public record SelfRefKeyedFeatureId(Guid Value);

[FromEvent<SelfRefKeyedFeatureAdded>]
[FromEvent<SelfRefKeyedSubFeatureAdded>(parentKey: nameof(SelfRefKeyedSubFeatureAdded.ParentFeatureId))]
[FromEvent<SelfRefKeyedFeatureUITemplateSet>(key: nameof(SelfRefKeyedFeatureUITemplateSet.FeatureId))]
public record SelfRefKeyedFeature(
    [Key] SelfRefKeyedFeatureId Id,
    string Name,
    string? UITemplateId,
    [ChildrenFrom<SelfRefKeyedSubFeatureAdded>(
        identifiedBy: nameof(SelfRefKeyedFeature.Id),
        parentKey: nameof(SelfRefKeyedSubFeatureAdded.ParentFeatureId))]
    IEnumerable<SelfRefKeyedFeature> SubFeatures);

[FromEvent<SelfRefKeyedModuleCreated>]
public record SelfRefKeyedModule(
    [Key] SelfRefKeyedModuleId Id,
    string Name,
    [ChildrenFrom<SelfRefKeyedFeatureAdded>(
        key: nameof(SelfRefKeyedFeatureAdded.FeatureId),
        identifiedBy: nameof(SelfRefKeyedFeature.Id),
        parentKey: nameof(SelfRefKeyedFeatureAdded.ModuleId))]
    IEnumerable<SelfRefKeyedFeature> Features);

#pragma warning restore SA1402
