// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.Reactors.for_ReactorScenario.when_constructing;

public class with_an_explicit_store_overriding_defaults_identity : given.an_identity_scenario
{
    EventStoreForTesting _explicitStore;

    void Establish()
    {
        _explicitStore = new(null, _artifacts, null, new ScenarioTime(), "explicit-store", "explicit-namespace");
        _scenario = new(_defaults, sideEffectHandlers: _handlers, eventStore: _explicitStore);
        RegisterRecorder();
    }

    async Task Because() => await _scenario.Given.ForEventSource(EventSourceId.New()).Events(new ReactorIdentityRecorded());

    [Fact] void should_use_the_explicit_store_for_side_effects() => _usedStore.ShouldBeSame(_explicitStore);
    [Fact] void should_deliver_the_explicit_store_name() => _recorder.Context.EventStore.ShouldEqual(_explicitStore.Name);
    [Fact] void should_deliver_the_explicit_namespace() => _recorder.Context.Namespace.ShouldEqual(_explicitStore.Namespace);

    void Destroy() => _explicitStore.Dispose();
}
