// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;

/// <summary>
/// A namespace-local singleton updated by close and every protected transaction.
/// </summary>
internal sealed class NamespaceMarkerRow
{
    /// <summary>Gets the table shared by namespace state, events, and effect receipts.</summary>
    public const string TableName = "NamespaceMarker";

    /// <summary>Gets or sets the singleton key.</summary>
    public int Id { get; set; } = 1;

    /// <summary>Gets or sets the namespace lifetime.</summary>
    public Guid Incarnation { get; set; }

    /// <summary>Gets or sets whether this lifetime admits transactions.</summary>
    public bool IsOpen { get; set; }

    /// <summary>Gets or sets the real conflict revision.</summary>
    public long Touch { get; set; }

    /// <summary>Maps the same row in both namespace and event-sequence contexts.</summary>
    /// <param name="modelBuilder">The model being built.</param>
    public static void Configure(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<NamespaceMarkerRow>();
        entity.ToTable(TableName).HasKey(row => row.Id);
        entity.Property(row => row.Id).ValueGeneratedNever();
    }
}
