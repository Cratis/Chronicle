// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Occurrences;

namespace Cratis.Chronicle.Testing.Occurrences.for_OccurrenceScenario.when_constructing;

public class with_explicit_store_identity : Specification
{
    Defaults _defaults;
    OccurrenceScenario<given.a_scenario.Handler> _scenario;
    OccurrenceRecordResult _recorded;

    void Establish()
    {
        var artifacts = new InMemoryClientArtifactsProvider
        {
            EventTypes = [typeof(given.a_scenario.EffectWasRecorded)],
            Triggers = [typeof(given.a_scenario.ImportRequested)]
        };
        _defaults = new(artifacts, null, null, null, "occurrence-store", "occurrence-namespace");
        _scenario = new(_defaults);
    }

    async Task Because() => _recorded = await _scenario.Record(new given.a_scenario.ImportRequested("one"), "request-one");

    [Fact] void should_use_the_defaults_store_name() => _scenario.EventStore.Name.ShouldEqual(_defaults.EventStore.Name);
    [Fact] void should_use_the_defaults_namespace() => _scenario.EventStore.Namespace.ShouldEqual(_defaults.EventStore.Namespace);
    [Fact] void should_record_in_the_named_store() => _recorded.Status.ShouldEqual(OccurrenceRecordStatus.Recorded);

    void Destroy()
    {
        _scenario.Dispose();
        ((IDisposable)_defaults.EventStore).Dispose();
    }
}
