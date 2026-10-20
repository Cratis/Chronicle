// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster;

/// <summary>
/// Configures provider-specific membership storage.
/// </summary>
internal static class MembershipModel
{
    /// <summary>
    /// Configures membership entities for the selected provider.
    /// </summary>
    /// <param name="builder">The model builder.</param>
    /// <param name="provider">The EF provider name.</param>
    internal static void Configure(ModelBuilder builder, string? provider)
    {
        var sqlServer = provider == "Microsoft.EntityFrameworkCore.SqlServer";
        var collation = provider switch
        {
            "Microsoft.EntityFrameworkCore.SqlServer" => "Latin1_General_100_BIN2",
            "Npgsql.EntityFrameworkCore.PostgreSQL" => "C",
            _ => "BINARY"
        };
        var version = builder.Entity<MembershipVersion>();
        version.ToTable(WellKnownTableNames.MembershipVersions);
        version.HasKey(row => row.ClusterId);
        version.Property(row => row.ClusterId).HasMaxLength(150).UseCollation(collation);
        version.Property(row => row.Timestamp).HasPrecision(3);
        var entry = builder.Entity<MembershipRow>();
        entry.ToTable(WellKnownTableNames.MembershipEntries);
        entry.HasKey(row => new { row.ClusterId, row.Address, row.Port, row.Generation });
        entry.HasOne<MembershipVersion>().WithMany().HasForeignKey(row => row.ClusterId).OnDelete(DeleteBehavior.Cascade);
        entry.Property(row => row.ClusterId).HasMaxLength(150).UseCollation(collation);
        entry.Property(row => row.Address).HasMaxLength(45).IsUnicode(!sqlServer);
        entry.Property(row => row.SiloName).HasMaxLength(150);
        entry.Property(row => row.HostName).HasMaxLength(150);
        entry.Property(row => row.StartTime).HasPrecision(3);
        entry.Property(row => row.IAmAliveTime).HasPrecision(3);
    }
}
