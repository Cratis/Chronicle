// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Testing.Events.for_TestingArtifactCatalog;

[Collection(nameof(Cratis.Chronicle.Testing.for_EventStoreForTesting.given.schema_sharing_collection))]
public class when_migrators_depend_on_services : Specification
{
    EventStoreForTesting _first;
    EventStoreForTesting _second;
    ServiceProvider _firstServices;
    ServiceProvider _secondServices;

    void Because()
    {
        _firstServices = new ServiceCollection().AddSingleton(MarkerFor("first")).BuildServiceProvider();
        _secondServices = new ServiceCollection().AddSingleton(MarkerFor("second")).BuildServiceProvider();
        _first = new(_firstServices, new InMemoryClientArtifactsProvider { EventTypes = [typeof(DependentRecorded)], EventTypeMigrators = [typeof(DependentRecordedMigration)] });
        _second = new(_secondServices, new InMemoryClientArtifactsProvider { EventTypes = [typeof(DependentRecorded)], EventTypeMigrators = [typeof(DependentRecordedMigration)] });
    }

    [Fact] void should_not_share_a_catalog() => (_first.Metadata is null && _second.Metadata is null).ShouldBeTrue();
    [Fact] void should_build_the_first_migration_from_its_own_services() => MarkerOf(_first).ShouldEqual("first");
    [Fact] void should_build_the_second_migration_from_its_own_services() => MarkerOf(_second).ShouldEqual("second");

    static Marker MarkerFor(string value) => new() { Value = value };

    static string MarkerOf(EventStoreForTesting store) =>
        ((DependentRecordedMigration)store.EventTypeMigrators.GetMigratorsFor(typeof(DependentRecorded)).Single()).Value;

    void Destroy()
    {
        _first.Dispose();
        _second.Dispose();
        _firstServices.Dispose();
        _secondServices.Dispose();
    }

    public class Marker
    {
        public string Value { get; init; } = "none";
    }

    [EventType(generation: 2)]
    public record DependentRecorded(string Value);

    [EventTypeGenerationFor<DependentRecorded>(1)]
    public record DependentRecordedV1;

    public class DependentRecordedMigration(Marker? marker = null) : EventTypeMigration<DependentRecorded, DependentRecordedV1>
    {
        public string Value => marker?.Value ?? "none";

        public override void Upcast(IEventMigrationBuilder<DependentRecorded, DependentRecordedV1> builder) =>
            builder.Properties(properties => properties.DefaultValue(recorded => recorded.Value, Value));

        public override void Downcast(IEventMigrationBuilder<DependentRecordedV1, DependentRecorded> builder) =>
            builder.Properties(_ => { });
    }
}
