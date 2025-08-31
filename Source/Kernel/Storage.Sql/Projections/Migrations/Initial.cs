// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.Projections.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(ProjectionsDbContext))]
[Migration("Projections-Initial")]
public class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Projections",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                Owner = table.Column<int>(type: "INTEGER", nullable: false),
                ReadModelName = table.Column<string>(type: "TEXT", nullable: false),
                ReadModelGeneration = table.Column<int>(type: "INTEGER", nullable: false),
                SinkType = table.Column<Guid>(type: "TEXT", nullable: false),
                SinkConfigurationId = table.Column<Guid>(type: "TEXT", nullable: false),
                Definitions = table.Column<IDictionary<string, string>>(type: "TEXT", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_Projections", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Projections");
    }
}

[DbContext(typeof(ProjectionsDbContext))]
public class InitialSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) => modelBuilder
        .Entity(typeof(Projection).FullName, b =>
        {
            b.Property<string>(nameof(Projection.Id))
                .HasColumnType("TEXT")
                .IsRequired();

            b.Property<EventTypeOwner>(nameof(Projection.Owner))
                .HasColumnType("INTEGER")
                .IsRequired();

            b.Property<string>(nameof(Projection.ReadModelName))
                .HasColumnType("TEXT")
                .IsRequired();

            b.Property<int>(nameof(Projection.ReadModelGeneration))
                .HasColumnType("INTEGER")
                .IsRequired();

            b.Property<Guid>(nameof(Projection.SinkType))
                .HasColumnType("TEXT")
                .IsRequired();

            b.Property<Guid>(nameof(Projection.SinkConfigurationId))
                .HasColumnType("TEXT")
                .IsRequired();

            b.Property<IDictionary<string, string>>(nameof(Projection.Definitions))
                .HasColumnType("TEXT")
                .IsRequired();

            b.HasKey(nameof(Projection.Id));
            b.ToTable("Projections");
        });
}
