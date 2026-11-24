// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_SinkProvenanceStorage;

public class when_credentials_and_payload_schemas_differ : Specification
{
    Exception? _rotation;
    string? _schema;
    bool _separateModels;

    void Because()
    {
        using var first = Context("first", "one", "payload_one", "canonical");
        using var rotated = Context("second", "two", "payload_two", "canonical");
        using var otherMetadataSchema = Context("second", "two", "payload_two", "other_canonical");
        _rotation = Catch.Exception(() => SinkProvenanceTarget.For(first).Ensure(rotated));
        _schema = rotated.Model.FindEntityType(typeof(DynamicReadModelEntity))!.GetSchema();
        _separateModels = !ReferenceEquals(rotated.Model, otherMetadataSchema.Model);
    }

    static ReadModelDbContext Context(string user, string password, string payloadSchema, string metadataSchema) => new(
        SpecDbContextOptions.Kernel<ReadModelDbContext>($"Host=localhost;Database=physical;Username={user};Password={password};Search Path={payloadSchema}"),
        SinkProvenanceNames.Inventory, SinkProvenanceSchema.InventoryColumns, Substitute.For<IReadModelMigrator>(), metadataSchema);

    [Fact] void should_not_treat_credential_rotation_as_relocation() => _rotation.ShouldBeNull();
    [Fact] void should_address_metadata_in_the_configured_storage_schema() => _schema.ShouldEqual("canonical");
    [Fact] void should_include_the_metadata_schema_in_model_cache_identity() => _separateModels.ShouldBeTrue();
}
