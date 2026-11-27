// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;

/// <summary>
/// A durable, event-store-level tombstone, independent of the removable namespace listing.
/// </summary>
internal sealed class NamespaceLifecycleRow
{
    /// <summary>
    /// The table retained across namespace removal and recreation.
    /// </summary>
    public const string TableName = "NamespaceLifecycles";

    /// <summary>
    /// Gets or sets the invariant-uppercase UTF-8 name, avoiding provider collation and padding differences.
    /// </summary>
    public byte[] NameKey { get; set; } = [];

    /// <summary>
    /// Gets or sets the monotonic compare-and-swap revision.
    /// </summary>
    public long Revision { get; set; }

    /// <summary>
    /// Gets or sets the complete recovery state, encoded with kernel-owned JSON options.
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// Maps the durable control table in the event-store context only.
    /// </summary>
    /// <param name="modelBuilder">The model being built.</param>
    public static void Configure(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<NamespaceLifecycleRow>();
        entity.ToTable(TableName).HasKey(row => row.NameKey);
        entity.Property(row => row.NameKey).HasMaxLength(800).ValueGeneratedNever();
        entity.Property(row => row.Payload).IsRequired();
    }
}
