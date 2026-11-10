// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Imports;

/// <summary>
/// Represents a staged import chunk row.
/// </summary>
public class ImportChunkEntity
{
    /// <summary>
    /// Gets or sets the import the chunk belongs to.
    /// </summary>
    public Guid ImportId { get; set; }

    /// <summary>
    /// Gets or sets the chunk index.
    /// </summary>
    public int ChunkIndex { get; set; }

    /// <summary>
    /// Gets or sets the index in the import of the first event in the chunk.
    /// </summary>
    public long FirstEventIndex { get; set; }

    /// <summary>
    /// Gets or sets how many events the chunk carried.
    /// </summary>
    public int EventsReceived { get; set; }

    /// <summary>
    /// Gets or sets how many events were staged.
    /// </summary>
    public int EventsStaged { get; set; }

    /// <summary>
    /// Gets or sets how many events were rejected while staging.
    /// </summary>
    public int EventsRejected { get; set; }

    /// <summary>
    /// Gets or sets the serialized chunk.
    /// </summary>
    public string Payload { get; set; } = string.Empty;
}
