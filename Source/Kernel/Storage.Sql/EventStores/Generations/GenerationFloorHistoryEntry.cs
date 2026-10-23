// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Generations;

/// <summary>A durable generation floor audit revision.</summary>
public class GenerationFloorHistoryEntry
{
    /// <summary>Gets or sets MinimumStorageProtocol.</summary>
    public int MinimumStorageProtocol { get; set; }

    /// <summary>Gets or sets MinimumWriterProtocol.</summary>
    public int MinimumWriterProtocol { get; set; }

    /// <summary>Gets or sets RequiredCapabilities.</summary>
    public string RequiredCapabilities { get; set; } = string.Empty;

    /// <summary>Gets or sets Incarnation.</summary>
    public Guid Incarnation { get; set; }

    /// <summary>Gets or sets Revision.</summary>
    public long Revision { get; set; }

    /// <summary>Gets or sets AdvancedAt.</summary>
    public DateTimeOffset AdvancedAt { get; set; }

    /// <summary>Gets or sets AdvancedBy.</summary>
    public string AdvancedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets Reason.</summary>
    public string Reason { get; set; } = string.Empty;
}
