// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

public class and_a_recursive_slice_uses_a_shared_event_source : Specification
{
    ReadModelScenario<ModuleWithSlicesSelfRef> _scenario;
    FeatureNodeId _subFeatureId;
    Guid _sliceId;
    ModuleWithSlicesSelfRef _module;

    void Establish()
    {
        _scenario = new ReadModelScenario<ModuleWithSlicesSelfRef>();
        _sliceId = Guid.NewGuid();
    }

    async Task Because()
    {
        var moduleId = ModuleNodeId.New();
        var featureId = FeatureNodeId.New();
        _subFeatureId = new FeatureNodeId(moduleId.Value);
        await _scenario.Given.ForEventSource(moduleId).Events(
            new ModuleCreatedSelfRef("Catalog"),
            new FeatureAddedSelfRef(moduleId, featureId, "Products"),
            new SubFeatureAddedSelfRef(featureId, "Inventory"),
            new StateChangeSliceAddedSelfRef(_subFeatureId, _sliceId, "Stock changed"));
        _module = _scenario.Instance!;
    }

    [Fact] void should_keep_the_root_feature() => _module.Features.Count.ShouldEqual(1);
    [Fact] void should_keep_the_sub_feature_identity() => _module.Features.Single().SubFeatures.Single().Id.ShouldEqual(_subFeatureId);
    [Fact] void should_set_the_sub_feature_name() => _module.Features.Single().SubFeatures.Single().Name.ShouldEqual("Inventory");
    [Fact] void should_attach_exactly_one_slice() => _module.Features.Single().SubFeatures.Single().Slices.Single().Id.ShouldEqual(_sliceId);
    [Fact] void should_set_the_slice_name() => _module.Features.Single().SubFeatures.Single().Slices.Single().Name.ShouldEqual("Stock changed");
    [Fact] void should_not_attach_the_slice_to_the_ancestor() => _module.Features.Single().Slices.ShouldBeEmpty();
    [Fact] void should_not_create_a_self_referencing_stub() => _module.Features.Single().SubFeatures.Single().SubFeatures.ShouldBeEmpty();

    void Destroy() => _scenario.Dispose();
}
