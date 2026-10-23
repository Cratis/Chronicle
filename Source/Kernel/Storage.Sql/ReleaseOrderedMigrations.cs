// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;
#pragma warning disable EF1001 // MigratorData is what the migrator hands its migrations over in; the migrators that reorder it are pinned by specs.
using Microsoft.EntityFrameworkCore.Migrations.Internal;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Puts the migrations a migrator is about to run in the order of the releases they were written for.
/// </summary>
/// <remarks>
/// EF Core orders the migrations it applies by their identifiers, compared as text. That runs a v19_104_0 migration before a v19_15_0 one, so
/// on a fresh database a migration could run before one it builds on. Migrations to apply are ordered by <see cref="MigrationReleaseOrder"/>
/// instead, and migrations to revert in the opposite order. Which migrations run is still EF Core's decision.
/// </remarks>
static class ReleaseOrderedMigrations
{
    /// <summary>
    /// Orders what a migrator is about to run by release.
    /// </summary>
    /// <param name="data">The <see cref="MigratorData"/> EF Core decided on.</param>
    /// <returns>The same migrations, in release order.</returns>
    public static MigratorData InReleaseOrder(MigratorData data) =>
        new(
            [.. data.AppliedMigrations.OrderBy(IdOf, MigrationReleaseOrder.Instance)],
            [.. data.RevertedMigrations.OrderByDescending(IdOf, MigrationReleaseOrder.Instance)],
            data.TargetMigration);

    static string IdOf(Migration migration) => migration.GetType().GetCustomAttribute<MigrationAttribute>()?.Id ?? string.Empty;
}
