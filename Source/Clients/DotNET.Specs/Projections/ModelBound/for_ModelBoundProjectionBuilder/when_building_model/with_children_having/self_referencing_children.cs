// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_having;

public class self_referencing_children : given.a_model_bound_projection_builder
{
    ProjectionDefinition _result;

    void Establish()
    {
        event_types = new EventTypesForSpecifications([
            typeof(SelfRefModuleCreated),
            typeof(SelfRefFeatureAdded),
            typeof(SelfRefSubFeatureAdded)
        ]);
        builder = new ModelBoundProjectionBuilder(naming_policy, event_types);
    }

    void Because() => _result = builder.Build(typeof(SelfRefModule));

    [Fact]
    void should_not_add_sub_feature_event_as_root_feature_creator()
    {
        var eventType = event_types.GetEventTypeFor(typeof(SelfRefSubFeatureAdded)).ToContract();
        var featureChildrenDef = _result.Children[nameof(SelfRefModule.Features)];
        featureChildrenDef.From.Keys.ShouldNotContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_add_sub_feature_event_to_nested_sub_features()
    {
        var eventType = event_types.GetEventTypeFor(typeof(SelfRefSubFeatureAdded)).ToContract();
        var featureChildrenDef = _result.Children[nameof(SelfRefModule.Features)];
        var subFeaturesDef = featureChildrenDef.Children[nameof(SelfRefFeature.SubFeatures)];
        subFeaturesDef.From.Keys.ShouldContain(et => et.IsEqual(eventType));
    }
}

[EventType]
public record SelfRefModuleCreated(string Name);

[EventType]
public record SelfRefFeatureAdded(SelfRefModuleId ModuleId, SelfRefFeatureId FeatureId, string Name);

[EventType]
public record SelfRefSubFeatureAdded(SelfRefFeatureId ParentFeatureId, string Name);

public record SelfRefModuleId(Guid Value);
public record SelfRefFeatureId(Guid Value);

[FromEvent<SelfRefFeatureAdded>]
[FromEvent<SelfRefSubFeatureAdded>(parentKey: nameof(SelfRefSubFeatureAdded.ParentFeatureId))]
public record SelfRefFeature(
    [Key] SelfRefFeatureId Id,
    string Name,
    [ChildrenFrom<SelfRefSubFeatureAdded>(
        identifiedBy: nameof(SelfRefFeature.Id),
        parentKey: nameof(SelfRefSubFeatureAdded.ParentFeatureId))]
    IEnumerable<SelfRefFeature> SubFeatures);

[FromEvent<SelfRefModuleCreated>]
public record SelfRefModule(
    [Key] SelfRefModuleId Id,
    string Name,
    [ChildrenFrom<SelfRefFeatureAdded>(
        key: nameof(SelfRefFeatureAdded.FeatureId),
        identifiedBy: nameof(SelfRefFeature.Id),
        parentKey: nameof(SelfRefFeatureAdded.ModuleId))]
    IEnumerable<SelfRefFeature> Features);

#pragma warning restore SA1402 // File may only contain a single type
