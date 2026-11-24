// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

public class and_slices_target_different_recursive_levels : Specification
{
    ReadModelScenario<ModuleWithSlicesSelfRef> _scenario;
    FeatureNodeId _deepFeatureId;
    Guid _topSliceId;
    Guid _deepSliceId;
    ModuleWithSlicesSelfRef _module;

    void Establish()
    {
        _scenario = new ReadModelScenario<ModuleWithSlicesSelfRef>();
        _deepFeatureId = FeatureNodeId.New();
        _topSliceId = Guid.NewGuid();
        _deepSliceId = Guid.NewGuid();
    }

    async Task Because()
    {
        var moduleId = ModuleNodeId.New();
        var featureId = FeatureNodeId.New();
        var subFeatureId = FeatureNodeId.New();
        await _scenario.Given.ForEventSource(moduleId).Events(new ModuleCreatedSelfRef("Catalog"));
        await _scenario.Given.ForEventSource(featureId).Events(new FeatureAddedSelfRef(moduleId, featureId, "Products"));
        await _scenario.Given.ForEventSource(subFeatureId).Events(new SubFeatureAddedSelfRef(featureId, "Inventory"));
        await _scenario.Given.ForEventSource(_deepFeatureId).Events(new SubFeatureAddedSelfRef(subFeatureId, "Warehouses"));
        await _scenario.Given.ForEventSource(_topSliceId).Events(new StateChangeSliceAddedSelfRef(featureId, _topSliceId, "Product changed"));
        await _scenario.Given.ForEventSource(_deepSliceId).Events(new StateChangeSliceAddedSelfRef(_deepFeatureId, _deepSliceId, "Warehouse changed"));
        _module = _scenario.Instance!;
    }

    [Fact] void should_keep_one_top_level_feature() => _module.Features.Count.ShouldEqual(1);
    [Fact] void should_preserve_the_deep_feature_identity() => DeepFeature().Id.ShouldEqual(_deepFeatureId);
    [Fact] void should_preserve_the_deep_feature_name() => DeepFeature().Name.ShouldEqual("Warehouses");
    [Fact] void should_route_the_top_level_slice() => _module.Features.Single().Slices.Single().Id.ShouldEqual(_topSliceId);
    [Fact] void should_route_the_deep_slice() => DeepFeature().Slices.Single().Id.ShouldEqual(_deepSliceId);
    [Fact] void should_set_the_deep_slice_name() => DeepFeature().Slices.Single().Name.ShouldEqual("Warehouse changed");
    [Fact] void should_leave_the_middle_level_unchanged() => _module.Features.Single().SubFeatures.Single().Slices.ShouldBeEmpty();
    [Fact] void should_not_create_a_self_referencing_stub() => DeepFeature().SubFeatures.ShouldBeEmpty();

    FeatureWithSlicesSelfRef DeepFeature() => _module.Features.Single().SubFeatures.Single().SubFeatures.Single();

    void Destroy() => _scenario.Dispose();
}
