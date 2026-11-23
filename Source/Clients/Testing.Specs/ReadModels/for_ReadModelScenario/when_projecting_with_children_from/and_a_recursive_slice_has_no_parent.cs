// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

public class and_a_recursive_slice_has_no_parent : Specification
{
    ReadModelScenario<ModuleWithSlicesSelfRef> _scenario;
    Exception _exception;

    void Establish() => _scenario = new ReadModelScenario<ModuleWithSlicesSelfRef>();

    async Task Because()
    {
        var moduleId = ModuleNodeId.New();
        var featureId = FeatureNodeId.New();
        var sliceId = Guid.NewGuid();
        await _scenario.Given.ForEventSource(moduleId).Events(new ModuleCreatedSelfRef("Catalog"));
        await _scenario.Given.ForEventSource(featureId).Events(new FeatureAddedSelfRef(moduleId, featureId, "Products"));
        await _scenario.Given.ForEventSource(sliceId).Events(new StateChangeSliceAddedSelfRef(FeatureNodeId.New(), sliceId, "Stock changed"));
        _exception = Catch.Exception(() => _ = _scenario.Instance);
    }

    [Fact] void should_still_report_the_missing_parent() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_silently_drop_the_slice() => _exception.Message.ShouldContain("Could not resolve parent key");

    void Destroy() => _scenario.Dispose();
}
