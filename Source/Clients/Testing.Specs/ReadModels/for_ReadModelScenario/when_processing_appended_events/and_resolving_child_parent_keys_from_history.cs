// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events;

public class and_resolving_child_parent_keys_from_history : appended_contexts
{
    ReadModelScenario<ModuleDashboard> _scenario;
    Guid _moduleId;
    Guid _featureId;
    Guid _sliceId;

    void Establish()
    {
        _scenario = new();
        _moduleId = Guid.NewGuid();
        _featureId = Guid.NewGuid();
        _sliceId = Guid.NewGuid();
    }

    Task Because() => _scenario.ProcessAppendedEvents(
    [
        new AppendedEvent(_context with { EventSourceId = new(_sliceId), SequenceNumber = 20UL }, new SliceAdded(_featureId, _sliceId, "Register")),
        new AppendedEvent(_context with { EventSourceId = new(_moduleId) }, new ModuleCreated("Module")),
        new AppendedEvent(_context with { EventSourceId = new(_featureId), SequenceNumber = 12UL }, new FeatureAdded(_moduleId, _featureId, "Feature"))
    ]);
    void Destroy() => _scenario.Dispose();

    [Fact] void should_materialize_one_root() => _scenario.Instances.Count.ShouldEqual(1);
    [Fact] void should_resolve_the_root_key() => _scenario.InstanceForEventSourceId(new(_moduleId))!.Name.ShouldEqual("Module");
    [Fact] void should_resolve_the_feature_parent() => _scenario.Instance!.Features.Single().Name.ShouldEqual("Feature");
    [Fact] void should_resolve_the_slice_parent_from_prior_events() => _scenario.Instance!.Features.Single().Slices.Single().Name.ShouldEqual("Register");
}
