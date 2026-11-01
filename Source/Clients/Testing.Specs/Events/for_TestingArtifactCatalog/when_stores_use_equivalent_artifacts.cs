// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Events.for_TestingArtifactCatalog;

[Collection(nameof(Cratis.Chronicle.Testing.for_EventStoreForTesting.given.schema_sharing_collection))]
public class when_stores_use_equivalent_artifacts : Specification
{
    EventStoreForTesting _first;
    EventStoreForTesting _second;
    ServiceProvider _services;

    void Because()
    {
        _services = new ServiceCollection().BuildServiceProvider();
        _first = new(null, new InMemoryClientArtifactsProvider { EventTypes = [typeof(Recorded)] });
        _second = new(_services, new InMemoryClientArtifactsProvider { EventTypes = [typeof(Recorded)] });
    }

    [Fact] void should_share_one_catalog() => _first.Metadata.ShouldBeSame(_second.Metadata);
    [Fact] void should_build_event_discovery_once() => _first.Metadata!.DiscoveryBuildCount.ShouldEqual(1);
    [Fact] void should_keep_schemas_local() => _first.JsonSchemaGenerator.Generate(typeof(Recorded)).ShouldNotBeSame(_second.JsonSchemaGenerator.Generate(typeof(Recorded)));

    void Destroy()
    {
        _first.Dispose();
        _second.Dispose();
        _services.Dispose();
    }

    [EventType("catalog-equivalent-recorded")]
    public record Recorded(string Value);
}
