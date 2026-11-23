// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

public class and_a_level_2_slice_is_removed : Specification
{
    ReadModelScenario<ModuleWithSlicesSelfRef> _scenario;
    Guid _topSliceId;
    Guid _sliceId;
    ModuleWithSlicesSelfRef _module;

    void Establish()
    {
        _scenario = new ReadModelScenario<ModuleWithSlicesSelfRef>();
        _topSliceId = Guid.NewGuid();
        _sliceId = Guid.NewGuid();
    }

    async Task Because()
    {
        var moduleId = ModuleNodeId.New();
        var featureId = FeatureNodeId.New();
        var subFeatureId = FeatureNodeId.New();
        await _scenario.Given.ForEventSource(moduleId).Events(new ModuleCreatedSelfRef("Catalog"));
        await _scenario.Given.ForEventSource(featureId).Events(new FeatureAddedSelfRef(moduleId, featureId, "Products"));
        await _scenario.Given.ForEventSource(subFeatureId).Events(new SubFeatureAddedSelfRef(featureId, "Inventory"));
        await _scenario.Given.ForEventSource(_topSliceId).Events(new StateChangeSliceAddedSelfRef(featureId, _topSliceId, "Product changed"));
        await _scenario.Given.ForEventSource(_sliceId).Events(
            new StateChangeSliceAddedSelfRef(subFeatureId, _sliceId, "Stock changed"),
            new StateChangeSliceRemovedSelfRef(_sliceId));
        _module = _scenario.Instance!;
    }

    [Fact] void should_remove_the_slice_under_the_sub_feature() => _module.Features.Single().SubFeatures.Single().Slices.ShouldBeEmpty();
    [Fact] void should_leave_the_top_level_slice_identity_unchanged() => _module.Features.Single().Slices.Single().Id.ShouldEqual(_topSliceId);
    [Fact] void should_leave_the_top_level_slice_name_unchanged() => _module.Features.Single().Slices.Single().Name.ShouldEqual("Product changed");
    [Fact] void should_preserve_the_sub_feature() => _module.Features.Single().SubFeatures.Single().Name.ShouldEqual("Inventory");
    [Fact] void should_not_create_recursive_stubs() => _module.Features.Single().SubFeatures.Single().SubFeatures.ShouldBeEmpty();

    void Destroy() => _scenario.Dispose();
}
