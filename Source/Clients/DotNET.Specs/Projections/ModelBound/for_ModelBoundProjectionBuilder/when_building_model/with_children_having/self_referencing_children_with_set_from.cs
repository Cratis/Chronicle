// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;

namespace Cratis.Chronicle.Projections.ModelBound.for_ModelBoundProjectionBuilder.when_building_model.with_children_having;

public class self_referencing_children_with_set_from : given.a_model_bound_projection_builder
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

    void Because() => _result = builder.Build(typeof(SelfRefModuleWithSetFrom));

    [Fact]
    void should_not_add_sub_feature_event_as_root_feature_creator()
    {
        var eventType = event_types.GetEventTypeFor(typeof(SelfRefSubFeatureAdded)).ToContract();
        var featureChildrenDef = _result.Children[nameof(SelfRefModuleWithSetFrom.Features)];
        featureChildrenDef.From.Keys.ShouldNotContain(et => et.IsEqual(eventType));
    }

    [Fact]
    void should_keep_sub_feature_mapping_on_nested_children()
    {
        var eventType = event_types.GetEventTypeFor(typeof(SelfRefSubFeatureAdded)).ToContract();
        var featureChildrenDef = _result.Children[nameof(SelfRefModuleWithSetFrom.Features)];
        var subFeaturesDef = featureChildrenDef.Children[nameof(SelfRefFeatureWithSetFrom.SubFeatures)];
        var fromDef = subFeaturesDef.From.Single(kvp => kvp.Key.IsEqual(eventType)).Value;
        fromDef.Properties[nameof(SelfRefFeatureWithSetFrom.Name)].ShouldEqual(nameof(SelfRefSubFeatureAdded.Name));
    }
}

[FromEvent<SelfRefFeatureAdded>]
[FromEvent<SelfRefSubFeatureAdded>(parentKey: nameof(SelfRefSubFeatureAdded.ParentFeatureId))]
public record SelfRefFeatureWithSetFrom(
    [Key] SelfRefFeatureId Id,
    [SetFrom<SelfRefSubFeatureAdded>(nameof(SelfRefSubFeatureAdded.Name))]
    string Name,
    [ChildrenFrom<SelfRefSubFeatureAdded>(
        identifiedBy: nameof(SelfRefFeatureWithSetFrom.Id),
        parentKey: nameof(SelfRefSubFeatureAdded.ParentFeatureId))]
    IEnumerable<SelfRefFeatureWithSetFrom> SubFeatures);

[FromEvent<SelfRefModuleCreated>]
public record SelfRefModuleWithSetFrom(
    [Key] SelfRefModuleId Id,
    string Name,
    [ChildrenFrom<SelfRefFeatureAdded>(
        key: nameof(SelfRefFeatureAdded.FeatureId),
        identifiedBy: nameof(SelfRefFeatureWithSetFrom.Id),
        parentKey: nameof(SelfRefFeatureAdded.ModuleId))]
    IEnumerable<SelfRefFeatureWithSetFrom> Features);

#pragma warning restore SA1402 // File may only contain a single type
