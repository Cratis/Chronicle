// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.Namespaces.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespacesDbContext))]
[Migration("Namespaces-Initial")]
public class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Namespaces",
            columns: table => new
            {
                Name = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Namespaces", x => x.Name));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Namespaces");
    }
}

[DbContext(typeof(NamespacesDbContext))]
public class InitialSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) => modelBuilder
        .Entity(typeof(Namespace).FullName, b =>
        {
            b.Property<string>(nameof(Namespace.Name))
                .HasColumnType("TEXT")
                .IsRequired();

            b.HasKey(nameof(Namespace.Name));
            b.ToTable("Namespaces");
        });
}
