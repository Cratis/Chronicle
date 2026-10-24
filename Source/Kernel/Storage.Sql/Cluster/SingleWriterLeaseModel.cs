// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Configures the kernel lease table.
/// </summary>
internal static class SingleWriterLeaseModel
{
    /// <summary>
    /// Configures the lease mapping for the current provider.
    /// </summary>
    /// <param name="builder">The model builder.</param>
    /// <param name="provider">The EF provider name.</param>
    internal static void Configure(ModelBuilder builder, string? provider)
    {
        var collation = provider switch
        {
            "Microsoft.EntityFrameworkCore.SqlServer" => "Latin1_General_100_BIN2",
            "Npgsql.EntityFrameworkCore.PostgreSQL" => "C",
            _ => "BINARY"
        };
        var lease = builder.Entity<SingleWriterLeaseRow>();
        lease.ToTable(WellKnownTableNames.SingleWriterLeases);
        lease.HasKey(row => row.Name);
        lease.Property(row => row.Name).HasMaxLength(64).UseCollation(collation);
        lease.Property(row => row.Owner).HasMaxLength(200).UseCollation(collation);
        lease.Property(row => row.ClusterId).HasMaxLength(150);
        lease.Property(row => row.ServiceId).HasMaxLength(150);
        lease.Property(row => row.HostName).HasMaxLength(150);
        lease.Property(row => row.Version).HasMaxLength(150);
        lease.Property(row => row.AcquiredAt).HasPrecision(3);
        lease.Property(row => row.RenewedAt).HasPrecision(3);
        lease.Property(row => row.ExpiresAt).HasPrecision(3);
    }
}
