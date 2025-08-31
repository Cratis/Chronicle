// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.Migrations.Cluster;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(ClusterDbContext))]
[Migration(nameof(Initial))]
public class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EventStores",
            columns: table => new
            {
                Name = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_EventStores", x => x.Name));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "EventStores");
    }
}

[DbContext(typeof(ClusterDbContext))]
public class InitialSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) => modelBuilder
        .Entity(typeof(EventStore).FullName, b =>
        {
            b.Property<string>(nameof(EventStore.Name))
                .HasColumnType("TEXT")
                .IsRequired();

            b.HasKey(nameof(EventStore.Name));

            b.ToTable("EventStores");
        });
}
