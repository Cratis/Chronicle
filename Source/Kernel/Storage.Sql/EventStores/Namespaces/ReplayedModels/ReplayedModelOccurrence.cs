// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReplayedModels;

/// <summary>
/// Represents a replayed model occurrence in the database.
/// </summary>
public class ReplayedModelOccurrence
{
    /// <summary>
    /// Gets or sets the observer identifier.
    /// </summary>
    [Key]
    public string ObserverId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the read model identifier.
    /// </summary>
    public string ReadModelIdentifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the read model name.
    /// </summary>
    public string ReadModelName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the revert model name.
    /// </summary>
    public string RevertModelName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the replay was started.
    /// </summary>
    public DateTimeOffset Started { get; set; }
}
