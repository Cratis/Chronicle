// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

public class and_self_referencing_children_span_multiple_levels : Specification
{
    ReadModelScenario<ModuleDashboardSelfRef> _scenario;
    ModuleNodeId _moduleId;
    FeatureNodeId _rootFeatureId;
    FeatureNodeId _level1FeatureId;
    FeatureNodeId _level2FeatureId;

    void Establish()
    {
        _scenario = new ReadModelScenario<ModuleDashboardSelfRef>();
        _moduleId = ModuleNodeId.New();
        _rootFeatureId = FeatureNodeId.New();
        _level1FeatureId = FeatureNodeId.New();
        _level2FeatureId = FeatureNodeId.New();
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(_moduleId)
            .Events(new ModuleCreatedSelfRef("Catalog"));

        await _scenario.Given
            .ForEventSource(_rootFeatureId)
            .Events(new FeatureAddedSelfRef(_moduleId, _rootFeatureId, "Root"));

        await _scenario.Given
            .ForEventSource(_level1FeatureId)
            .Events(new SubFeatureAddedSelfRef(_rootFeatureId, "L1"));

        await _scenario.Given
            .ForEventSource(_level2FeatureId)
            .Events(new SubFeatureAddedSelfRef(_level1FeatureId, "L2"));
    }

    [Fact] void should_have_root_feature_at_root() => _scenario.Instance.Features.Count().ShouldEqual(1);
    [Fact] void should_have_level1_nested_under_root() => _scenario.Instance.Features.Single().SubFeatures.Count().ShouldEqual(1);
    [Fact] void should_have_level2_nested_under_level1() => _scenario.Instance.Features.Single().SubFeatures.Single().SubFeatures.Count().ShouldEqual(1);
    [Fact] void should_set_level2_name() => _scenario.Instance.Features.Single().SubFeatures.Single().SubFeatures.Single().Name.ShouldEqual("L2");
}
