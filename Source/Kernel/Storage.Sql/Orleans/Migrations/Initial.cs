// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.Orleans.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(OrleansDbContext))]
[Migration("Orleans-Initial")]
public class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Reminders",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                GrainId = table.Column<string>(type: "TEXT", nullable: false),
                ReminderName = table.Column<string>(type: "TEXT", nullable: false),
                Period = table.Column<long>(type: "INTEGER", nullable: false),
                StartAt = table.Column<DateTime>(type: "INTEGER", nullable: false),
                ETag = table.Column<string>(type: "TEXT", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_Reminders", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Reminders");
    }
}

[DbContext(typeof(OrleansDbContext))]
public class InitialSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) => modelBuilder
        .Entity(typeof(Reminder).FullName, b =>
        {
            b.Property<string>(nameof(Reminder.Id))
                .HasColumnType("TEXT")
                .IsRequired();
            b.Property<string>(nameof(Reminder.GrainId))
                .HasColumnType("TEXT")
                .IsRequired();
            b.Property<string>(nameof(Reminder.ReminderName))
                .HasColumnType("TEXT")
                .IsRequired();
            b.Property<long>(nameof(Reminder.Period))
                .HasColumnType("INTEGER")
                .IsRequired();
            b.Property<DateTime>(nameof(Reminder.StartAt))
                .HasColumnType("INTEGER")
                .IsRequired();
            b.Property<string>(nameof(Reminder.ETag))
                .HasColumnType("TEXT")
                .IsRequired();

            b.HasKey(nameof(Reminder.Id));
            b.ToTable("Reminders");
        });
}
