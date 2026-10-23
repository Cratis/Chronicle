// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.for_ReleaseOrderedMigrator.given;

public class a_context_with_migrations_from_three_releases : Specification
{
    protected ReleasesDbContext _context;
    string _path;

    void Establish()
    {
        var directory = Path.Combine(Path.GetTempPath(), "chronicle-specs", "release-ordered-migrations");
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, $"{Guid.NewGuid():N}.db");
        var options = Database.BuildOptions<ReleasesDbContext>(new ServiceCollection().BuildServiceProvider(), $"Data Source={_path};Pooling=False");
        _context = new ReleasesDbContext(new DbContextOptionsBuilder<ReleasesDbContext>(options)
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options);
    }

    protected string[] AppliedMigrations() =>
        [.. _context.Database.SqlQueryRaw<string>("SELECT MigrationId AS Value FROM __EFMigrationsHistory ORDER BY rowid")];

    void Destroy()
    {
        _context.Dispose();
        File.Delete(_path);
    }
}
