// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Generations;

/// <summary>
/// Stores initialization audit evidence.
/// </summary>
public class GenerationInitializationReceiptRow
{
    /// <summary>
    /// Gets or sets Step.
    /// </summary>
    public int Step { get; set; }

    /// <summary>
    /// Gets or sets Subject.
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets EventStore.
    /// </summary>
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets Evidence.
    /// </summary>
    public string Evidence { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets Incarnation.
    /// </summary>
    public Guid Incarnation { get; set; }

    /// <summary>
    /// Gets or sets FloorRevision.
    /// </summary>
    public long FloorRevision { get; set; }

    /// <summary>
    /// Gets or sets RecordedAt.
    /// </summary>
    public DateTimeOffset RecordedAt { get; set; }
}
