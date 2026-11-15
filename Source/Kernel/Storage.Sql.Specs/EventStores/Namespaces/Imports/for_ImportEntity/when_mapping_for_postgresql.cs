// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Imports.for_ImportEntity;

/// <summary>
/// The import tables keep their payloads in a jsonb column on PostgreSQL. The model has to say so, or PostgreSQL refuses
/// to store the text the model writes there.
/// </summary>
public class when_mapping_for_postgresql : Specification
{
    string? _importPayload;
    string? _chunkPayload;

    void Because()
    {
        using var context = new NamespaceDbContext(SpecDbContextOptions.Kernel<NamespaceDbContext>("Host=localhost;Database=imports_mapping;Username=test;Password=test"));
        _importPayload = context.Model.FindEntityType(typeof(ImportEntity))!.FindProperty(nameof(ImportEntity.Payload))!.GetColumnType();
        _chunkPayload = context.Model.FindEntityType(typeof(ImportChunkEntity))!.FindProperty(nameof(ImportChunkEntity.Payload))!.GetColumnType();
    }

    [Fact] void should_map_the_import_payload_to_jsonb() => _importPayload.ShouldEqual("jsonb");
    [Fact] void should_map_the_chunk_payload_to_jsonb() => _chunkPayload.ShouldEqual("jsonb");
}
