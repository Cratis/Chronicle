// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_projecting_with_children_from;

public class and_events_on_separate_event_sources : Specification
{
    ReadModelScenario<ModuleDashboard> _scenario;
    EventSourceId _moduleId;
    EventSourceId _featureId;
    EventSourceId _sliceId;
    Guid _moduleGuid;
    Guid _featureGuid;
    Guid _sliceGuid;

    void Establish()
    {
        _scenario = new ReadModelScenario<ModuleDashboard>();
        _moduleGuid = Guid.NewGuid();
        _featureGuid = Guid.NewGuid();
        _sliceGuid = Guid.NewGuid();
        _moduleId = new EventSourceId(_moduleGuid);
        _featureId = new EventSourceId(_featureGuid);
        _sliceId = new EventSourceId(_sliceGuid);
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(_moduleId)
            .Events(new ModuleCreated("My Module"));

        await _scenario.Given
            .ForEventSource(_featureId)
            .Events(new FeatureAdded(_moduleGuid, _featureGuid, "My Feature"));

        await _scenario.Given
            .ForEventSource(_sliceId)
            .Events(new SliceAdded(_featureGuid, _sliceGuid, "Register Something"));
    }

    [Fact] void should_have_an_instance() => _scenario.Instance.ShouldNotBeNull();
    [Fact] void should_map_module_name() => _scenario.Instance!.Name.ShouldEqual("My Module");
    [Fact] void should_have_one_feature() => _scenario.Instance!.Features.Count().ShouldEqual(1);
    [Fact] void should_map_feature_name() => _scenario.Instance!.Features.First().Name.ShouldEqual("My Feature");
    [Fact] void should_have_one_slice() => _scenario.Instance!.Features.First().Slices.Count().ShouldEqual(1);
    [Fact] void should_map_slice_name() => _scenario.Instance!.Features.First().Slices.First().Name.ShouldEqual("Register Something");
}
