// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Events.for_TestingArtifactCatalog;

[Collection(nameof(Cratis.Chronicle.Testing.for_EventStoreForTesting.given.schema_sharing_collection))]
public class when_migrators_are_parameterless : Specification
{
    EventStoreForTesting _first;
    EventStoreForTesting _second;
    ServiceProvider _services;

    void Because()
    {
        _services = new ServiceCollection().BuildServiceProvider();
        _first = new(null, new InMemoryClientArtifactsProvider { EventTypes = [typeof(ParameterlessRecorded)], EventTypeMigrators = [typeof(ParameterlessRecordedMigration)] });
        _second = new(_services, new InMemoryClientArtifactsProvider { EventTypes = [typeof(ParameterlessRecorded)], EventTypeMigrators = [typeof(ParameterlessRecordedMigration)] });
    }

    [Fact] void should_share_one_catalog() => _first.Metadata.ShouldBeSame(_second.Metadata);

    void Destroy()
    {
        _first.Dispose();
        _second.Dispose();
        _services.Dispose();
    }

    [EventType(generation: 2)]
    public record ParameterlessRecorded(string Value);

    [EventTypeGenerationFor<ParameterlessRecorded>(1)]
    public record ParameterlessRecordedV1;

    public class ParameterlessRecordedMigration : EventTypeMigration<ParameterlessRecorded, ParameterlessRecordedV1>
    {
        public override void Upcast(IEventMigrationBuilder<ParameterlessRecorded, ParameterlessRecordedV1> builder) =>
            builder.Properties(properties => properties.DefaultValue(recorded => recorded.Value, "x"));

        public override void Downcast(IEventMigrationBuilder<ParameterlessRecordedV1, ParameterlessRecorded> builder) =>
            builder.Properties(_ => { });
    }
}
