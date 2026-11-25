// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

public class and_a_slice_precedes_its_sub_feature : Specification
{
    ReadModelScenario<ModuleWithSlicesSelfRef> _scenario;
    FeatureNodeId _subFeatureId;
    Guid _sliceId;
    ModuleWithSlicesSelfRef _module;

    void Establish()
    {
        _scenario = new ReadModelScenario<ModuleWithSlicesSelfRef>();
        _subFeatureId = FeatureNodeId.New();
        _sliceId = Guid.NewGuid();
    }

    async Task Because()
    {
        var moduleId = ModuleNodeId.New();
        var featureId = FeatureNodeId.New();
        var siblingId = FeatureNodeId.New();
        await _scenario.Given.ForEventSource(moduleId).Events(new ModuleCreatedSelfRef("Catalog"));
        await _scenario.Given.ForEventSource(featureId).Events(new FeatureAddedSelfRef(moduleId, featureId, "Products"));
        await _scenario.Given.ForEventSource(_sliceId).Events(new StateChangeSliceAddedSelfRef(_subFeatureId, _sliceId, "Stock changed"));
        await _scenario.Given.ForEventSource(_subFeatureId).Events(new SubFeatureAddedSelfRef(featureId, "Inventory"));
        await _scenario.Given.ForEventSource(siblingId).Events(new SubFeatureAddedSelfRef(featureId, "Pricing"));
        _module = _scenario.Instance!;
    }

    [Fact] void should_keep_one_top_level_feature() => _module.Features.Count.ShouldEqual(1);
    [Fact] void should_keep_both_sub_features() => _module.Features.Single().SubFeatures.Count.ShouldEqual(2);
    [Fact] void should_replay_the_slice_into_its_actual_parent() => Target().Slices.Single().Id.ShouldEqual(_sliceId);
    [Fact] void should_preserve_the_slice_name() => Target().Slices.Single().Name.ShouldEqual("Stock changed");
    [Fact] void should_not_create_an_empty_parent_stub() => Target().Name.ShouldEqual("Inventory");
    [Fact] void should_not_mutate_the_sibling() => _module.Features.Single().SubFeatures.Single(feature => feature.Id != _subFeatureId).Slices.ShouldBeEmpty();
    [Fact] void should_not_attach_the_slice_to_the_ancestor() => _module.Features.Single().Slices.ShouldBeEmpty();
    [Fact] void should_not_create_a_self_referencing_stub() => Target().SubFeatures.ShouldBeEmpty();

    FeatureWithSlicesSelfRef Target() => _module.Features.Single().SubFeatures.Single(feature => feature.Id == _subFeatureId);

    void Destroy() => _scenario.Dispose();
}
