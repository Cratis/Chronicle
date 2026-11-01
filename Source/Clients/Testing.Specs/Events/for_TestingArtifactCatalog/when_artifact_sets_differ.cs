// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.Events.for_TestingArtifactCatalog;

[Collection(nameof(Cratis.Chronicle.Testing.for_EventStoreForTesting.given.schema_sharing_collection))]
public class when_artifact_sets_differ : Specification
{
    EventStoreForTesting _first;
    EventStoreForTesting _second;

    void Because()
    {
        _first = new(null, new InMemoryClientArtifactsProvider { EventTypes = [typeof(FirstRecorded)] });
        _second = new(null, new InMemoryClientArtifactsProvider { EventTypes = [typeof(SecondRecorded)] });
    }

    [Fact] void should_have_separate_catalogs() => _first.Metadata.ShouldNotBeSame(_second.Metadata);
    [Fact] void should_build_the_first_catalog_once() => _first.Metadata!.DiscoveryBuildCount.ShouldEqual(1);
    [Fact] void should_build_the_second_catalog_once() => _second.Metadata!.DiscoveryBuildCount.ShouldEqual(1);
    [Fact] void should_not_discover_the_other_stores_events() => _second.EventTypes.HasFor(typeof(FirstRecorded)).ShouldBeFalse();

    void Destroy()
    {
        _first.Dispose();
        _second.Dispose();
    }

    [EventType("catalog-different-first-recorded")] public record FirstRecorded(string Value);
    [EventType("catalog-different-second-recorded")] public record SecondRecorded(string Value);
}
