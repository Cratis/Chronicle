// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Migrations;

#pragma warning disable SA1600
[DbContext(typeof(ClusterDbContext))]
[Migration("Cluster-ResourceNameReservations-v19_116_1")]
public class v19_116_1 : Migration
{
    const int KeyLength = 450;

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var collation = migrationBuilder.GetDatabaseType() switch
        {
            DatabaseType.SqlServer => "Latin1_General_100_BIN2",
            DatabaseType.PostgreSql => "C",
            _ => "BINARY"
        };
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.ResourceNameReservations,
            columns: table => new
            {
                Key = Binary(table.StringColumn(migrationBuilder, maxLength: KeyLength, nullable: false), collation),
                Owner = Binary(table.StringColumn(migrationBuilder, maxLength: KeyLength, nullable: false), collation)
            },
            constraints: table => table.PrimaryKey("PK_ResourceNameReservations", row => row.Key));
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(WellKnownTableNames.ResourceNameReservations);

    /// <summary>Arc's StringColumn has no collation parameter, so set it on the operation the helper created.</summary>
    /// <param name="column">The column operation the helper created.</param>
    /// <param name="collation">The binary collation to use.</param>
    /// <returns>The same column operation.</returns>
    static OperationBuilder<AddColumnOperation> Binary(OperationBuilder<AddColumnOperation> column, string collation)
    {
        column.GetInfrastructure().Collation = collation;
        return column;
    }
}
