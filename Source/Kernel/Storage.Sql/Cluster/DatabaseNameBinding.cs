// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Stores a logical identity's permanent physical database name.
/// </summary>
internal sealed class DatabaseNameBinding
{
    /// <summary>
    /// Gets or sets the owning schema.
    /// </summary>
    public DatabaseKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the event store name.
    /// </summary>
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the explicit namespace name.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the bound database name.
    /// </summary>
    public string PhysicalName { get; set; } = string.Empty;

    /// <summary>
    /// Gets the logical identity.
    /// </summary>
    internal DatabaseIdentity Identity => new(Kind, EventStore, Namespace);
}
