// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_constructing;

public class with_explicit_store_identity : given.an_identity_scenario
{
    void Establish()
    {
        _scenario = new(_defaults, sideEffectHandlers: _handlers);
        RegisterRecorder();
    }

    async Task Because() => await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new ReactorIdentityRecorded());

    [Fact] void should_create_the_store_with_the_defaults_name() => _usedStore.Name.ShouldEqual(_defaults.EventStore.Name);
    [Fact] void should_create_the_store_with_the_defaults_namespace() => _usedStore.Namespace.ShouldEqual(_defaults.EventStore.Namespace);
    [Fact] void should_deliver_the_defaults_store_name() => _recorder.Context.EventStore.ShouldEqual(_defaults.EventStore.Name);
    [Fact] void should_deliver_the_defaults_namespace() => _recorder.Context.Namespace.ShouldEqual(_defaults.EventStore.Namespace);
    [Fact] async Task should_append_side_effects_to_that_store() => (await _usedStore.EventLog.GetFromSequenceNumber(EventSequenceNumber.First)).Count.ShouldEqual(1);
}
