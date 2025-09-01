// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.ReadModels.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(ReadModelsDbContext))]
[Migration("ReadModels-Initial")]
public class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ReadModels",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                Owner = table.Column<int>(type: "INTEGER", nullable: false),
                Schemas = table.Column<IDictionary<string, string>>(type: "TEXT", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_ReadModels", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ReadModels");
    }
}

[DbContext(typeof(ReadModelsDbContext))]
public class InitialSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) => modelBuilder
        .Entity(typeof(ReadModel).FullName, b =>
        {
            b.Property<string>(nameof(ReadModel.Id))
                .HasColumnType("TEXT")
                .IsRequired();

            b.Property<EventTypeOwner>(nameof(ReadModel.Owner))
                .HasColumnType("INTEGER")
                .IsRequired();

            b.Property<IDictionary<string, string>>(nameof(ReadModel.Schemas))
                .HasColumnType("TEXT")
                .IsRequired();

            b.HasKey(nameof(ReadModel.Id));
            b.ToTable("ReadModels");
        });
}
