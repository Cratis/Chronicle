// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Represents an <see cref="IMigrationsAssembly"/> that lists a context's migrations in the order of the releases they were written for.
/// </summary>
/// <remarks>
/// EF Core applies pending migrations in the order <see cref="Migrations"/> lists them, which by default is the ordinal order of their
/// identifiers. That order puts v19_104_0 before v19_15_0, so on a fresh database a migration could run before one it builds
/// on. <see cref="MigrationReleaseOrder"/> decides the order instead. Identifiers are unchanged, so the history of an existing database
/// still matches.
/// </remarks>
/// <param name="currentContext">The <see cref="ICurrentDbContext"/> the migrations belong to.</param>
/// <param name="options">The <see cref="IDbContextOptions"/> naming the assembly that holds the migrations, if not the context's own.</param>
/// <param name="idGenerator">The <see cref="IMigrationsIdGenerator"/> that gets the name part of an identifier.</param>
public class ReleaseOrderedMigrationsAssembly(
    ICurrentDbContext currentContext,
    IDbContextOptions options,
    IMigrationsIdGenerator idGenerator) : IMigrationsAssembly
{
    readonly Type _contextType = currentContext.Context.GetType();
    IReadOnlyDictionary<string, TypeInfo>? _migrations;
    ModelSnapshot? _modelSnapshot;
    bool _modelSnapshotResolved;

    /// <inheritdoc/>
    public Assembly Assembly { get; } = MigrationsAssemblyFor(currentContext.Context.GetType(), options);

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, TypeInfo> Migrations => _migrations ??= FindMigrations();

    /// <inheritdoc/>
    public ModelSnapshot? ModelSnapshot
    {
        get
        {
            if (!_modelSnapshotResolved)
            {
                _modelSnapshot = FindModelSnapshot();
                _modelSnapshotResolved = true;
            }

            return _modelSnapshot;
        }
    }

    /// <inheritdoc/>
    public string? FindMigrationId(string nameOrId) =>
        Migrations.Keys.FirstOrDefault(id =>
            string.Equals(id, nameOrId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(idGenerator.GetName(id), nameOrId, StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc/>
    public Migration CreateMigration(TypeInfo migrationClass, string activeProvider)
    {
        var migration = (Migration)Activator.CreateInstance(migrationClass.AsType())!;
        migration.ActiveProvider = activeProvider;
        return migration;
    }

    static Assembly MigrationsAssemblyFor(Type contextType, IDbContextOptions options)
    {
        var assemblyName = RelationalOptionsExtension.Extract(options).MigrationsAssembly;
        return assemblyName is null ? contextType.Assembly : Assembly.Load(new AssemblyName(assemblyName));
    }

    OrderedDictionary<string, TypeInfo> FindMigrations()
    {
        var migrations = new OrderedDictionary<string, TypeInfo>();
        var entries = ConstructibleTypes()
            .Where(type => type.IsSubclassOf(typeof(Migration)) && BelongsToContext(type))
            .Select(type => KeyValuePair.Create(type.GetCustomAttribute<MigrationAttribute>()?.Id ?? string.Empty, type))
            .Where(entry => entry.Key.Length > 0)
            .OrderBy(entry => entry.Key, MigrationReleaseOrder.Instance);
        foreach (var entry in entries)
        {
            migrations.Add(entry.Key, entry.Value);
        }

        return migrations;
    }

    ModelSnapshot? FindModelSnapshot() =>
        ConstructibleTypes()
            .Where(type => type.IsSubclassOf(typeof(ModelSnapshot)) && BelongsToContext(type))
            .Select(type => (ModelSnapshot)Activator.CreateInstance(type.AsType())!)
            .FirstOrDefault();

    IEnumerable<TypeInfo> ConstructibleTypes() => Assembly.DefinedTypes.Where(type => !type.IsAbstract && !type.IsGenericTypeDefinition);

    bool BelongsToContext(TypeInfo type) => type.GetCustomAttribute<DbContextAttribute>()?.ContextType == _contextType;
}
