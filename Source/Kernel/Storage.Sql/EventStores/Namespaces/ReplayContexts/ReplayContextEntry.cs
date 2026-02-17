// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReplayContexts;

/// <summary>
/// Represents the entity for replay context entries in the database.
/// </summary>
public class ReplayContextEntry
{
    /// <summary>
    /// Gets or sets the read model identifier.
    /// </summary>
    [Key]
    public string ReadModelIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the read model generation.
    /// </summary>
    public uint Generation { get; set; }

    /// <summary>
    /// Gets or sets the read model name.
    /// </summary>
    public string ReadModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the revert model name.
    /// </summary>
    public string RevertModel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the started date and time.
    /// </summary>
    public DateTimeOffset Started { get; set; }
}
