// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

public class and_self_referencing_children_are_on_separate_event_sources : Specification
{
    ReadModelScenario<ModuleDashboardSelfRef> _scenario;
    ModuleNodeId _moduleId;
    FeatureNodeId _featureId;
    FeatureNodeId _subFeatureId;

    void Establish()
    {
        _scenario = new ReadModelScenario<ModuleDashboardSelfRef>();
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
            .Events(new SubFeatureAddedSelfRef(_featureId, _subFeatureId, "Inventory"));
    }

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_project_both_feature_events() => _scenario.Instance!.Features.Count().ShouldEqual(2);
    [Fact] void should_contain_sub_feature_event_name() => _scenario.Instance!.Features.Any(_ => _.Name == "Inventory").ShouldBeTrue();
}
