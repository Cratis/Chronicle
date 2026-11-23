// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

public class and_a_slice_targets_a_level_2_sub_feature : Specification
{
    ReadModelScenario<ModuleWithSlicesSelfRef> _scenario;
    ModuleNodeId _moduleId;
    FeatureNodeId _featureId;
    FeatureNodeId _subFeatureId;
    Guid _sliceId;
    ModuleWithSlicesSelfRef _module;

    void Establish()
    {
        _scenario = new ReadModelScenario<ModuleWithSlicesSelfRef>();
        _moduleId = ModuleNodeId.New();
        _featureId = FeatureNodeId.New();
        _subFeatureId = FeatureNodeId.New();
        _sliceId = Guid.NewGuid();
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(_moduleId).Events(new ModuleCreatedSelfRef("Catalog"));
        await _scenario.Given.ForEventSource(_featureId).Events(new FeatureAddedSelfRef(_moduleId, _featureId, "Products"));
        await _scenario.Given.ForEventSource(_subFeatureId).Events(new SubFeatureAddedSelfRef(_featureId, "Inventory"));
        await _scenario.Given.ForEventSource(_sliceId).Events(new StateChangeSliceAddedSelfRef(_subFeatureId, _sliceId, "Stock changed"));
        _module = _scenario.Instance!;
    }

    [Fact] void should_keep_only_the_top_level_feature_at_root() => _module.Features.Count.ShouldEqual(1);
    [Fact] void should_preserve_the_sub_feature_identifier() => _module.Features.Single().SubFeatures.Single().Id.ShouldEqual(_subFeatureId);
    [Fact] void should_preserve_the_sub_feature_name() => _module.Features.Single().SubFeatures.Single().Name.ShouldEqual("Inventory");
    [Fact] void should_attach_the_slice_to_the_sub_feature() => _module.Features.Single().SubFeatures.Single().Slices.Single().Id.ShouldEqual(_sliceId);
    [Fact] void should_set_the_slice_name() => _module.Features.Single().SubFeatures.Single().Slices.Single().Name.ShouldEqual("Stock changed");
    [Fact] void should_not_attach_the_slice_to_the_top_level_feature() => _module.Features.Single().Slices.ShouldBeEmpty();
    [Fact] void should_not_create_self_referencing_stubs() => _module.Features.Single().SubFeatures.Single().SubFeatures.ShouldBeEmpty();

    void Destroy() => _scenario.Dispose();
}
