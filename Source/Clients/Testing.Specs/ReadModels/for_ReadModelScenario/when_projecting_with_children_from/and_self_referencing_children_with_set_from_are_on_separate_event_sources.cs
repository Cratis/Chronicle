// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

public class and_self_referencing_children_with_set_from_are_on_separate_event_sources : Specification
{
    ReadModelScenario<ModuleDashboardSelfRefWithSetFrom> _scenario;
    ModuleNodeId _moduleId;
    FeatureNodeId _featureId;
    FeatureNodeId _subFeatureId;

    void Establish()
    {
        _scenario = new ReadModelScenario<ModuleDashboardSelfRefWithSetFrom>();
        _moduleId = ModuleNodeId.New();
        _featureId = FeatureNodeId.New();
        _subFeatureId = FeatureNodeId.New();
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(_moduleId)
            .Events(new ModuleCreatedSelfRef("Catalog"));

        await _scenario.Given
            .ForEventSource(_featureId)
            .Events(new FeatureAddedSelfRef(_moduleId, _featureId, "Products"));

        await _scenario.Given
            .ForEventSource(_subFeatureId)
            .Events(new SubFeatureAddedSelfRef(_featureId, "Inventory"));
    }

    [Fact] void should_only_have_parent_feature_at_root() => _scenario.Instance.Features.Count().ShouldEqual(1);
    [Fact] void should_nest_sub_feature_under_parent() => _scenario.Instance.Features.Single().SubFeatures.Count().ShouldEqual(1);
    [Fact] void should_set_sub_feature_name() => _scenario.Instance.Features.Single().SubFeatures.Single().Name.ShouldEqual("Inventory");
}
