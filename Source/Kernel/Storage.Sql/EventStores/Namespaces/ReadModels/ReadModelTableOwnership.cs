// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Concepts.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Reads existing definitions and replay history without migrating or opening a dynamic model table.
/// Unknown placement must be included conservatively by the scope enumerator; metadata that cannot be read makes its scope unknown, never empty.
/// </summary>
/// <param name="scopes">Enumerates borrowed metadata contexts for every potentially shared logical scope.</param>
/// <param name="context">The metadata-only context at the resolved read-model placement.</param>
/// <param name="catalog">The exact catalog for that placement.</param>
/// <param name="shape">Checks the expected model column/key shape without altering it.</param>
internal sealed class ReadModelTableOwnership(
    Func<Task<IReadOnlyList<ReadModelTableMetadataScope>>> scopes,
    ReadModelTableBindingsDbContext context,
    IReadModelTableCatalog catalog,
    Func<string, Task<bool>> shape) : IReadModelTableOwnership
{
    /// <inheritdoc/>
    public async Task<ReadModelTableClaims> Read()
    {
        var claims = new List<ReadModelTableClaim>();
        var unknown = new List<ReadModelTableMetadataScope>();
        var borrowed = await scopes();
        try
        {
            foreach (var scope in borrowed)
            {
                await ReadScope(scope, claims, unknown);
            }
        }
        finally
        {
            foreach (var scope in borrowed)
            {
                await scope.DisposeAsync();
            }
        }

        if (await catalog.Find(WellKnownTableNames.ReadModelTableNames) is not null)
        {
            claims.AddRange((await context.Bindings.AsNoTracking().ToArrayAsync()).Select(binding => new ReadModelTableClaim(binding.Owner)));
        }

        if (await catalog.Find(WellKnownTableNames.ReplayPromotions) is not null)
        {
            foreach (var marker in await context.ReplayPromotions.AsNoTracking().ToArrayAsync())
            {
                AddPromotion(marker, claims);
            }
        }

        return new(claims, unknown);
    }

    /// <inheritdoc/>
    public Task<bool> HasExpectedShape(string physicalName) => shape(physicalName);

    static void AddReplayClaims(ReadModelTableMetadataScope scope, List<ReadModelTableClaim> claims, string model, string container, string revert, bool occurred)
    {
        var live = new ReadModelTableOwner(scope.EventStoreId, scope.Namespace!, model, container, ReadModelTableRole.Live, container);
        claims.Add(new(live));
        claims.Add(new(live with { Role = ReadModelTableRole.Replay, LogicalName = ReadModelTableNames.Replay(container) }));
        claims.Add(new(live with { Role = ReadModelTableRole.Backup, LogicalName = revert }, Occurred: occurred));
    }

    static void AddPromotion(ReplayPromotion marker, List<ReadModelTableClaim> claims)
    {
        var owners = claims.Where(claim => claim.Owner.Role == ReadModelTableRole.Backup &&
            claim.Owner.ContainerName == marker.ContainerName && claim.Owner.LogicalName == marker.RevertContainerName)
            .Select(claim => claim.Owner).Distinct().ToArray();
        if (owners.Length != 1 || marker.Id != ReplayPromotion.IdentifierFor(marker.ContainerName, marker.RevertContainerName))
        {
            // Markers predate logical scope fields. Shared placement cannot manufacture their attribution.
            throw new InvalidOperationException("A replay promotion cannot be attributed to exactly one scoped model owner; recovery required.");
        }
        claims.Add(new(owners[0], PromotedPhysicalName: marker.BackupTableName, Displaced: marker.BackupTableName is null));
    }

    async Task ReadScope(ReadModelTableMetadataScope scope, List<ReadModelTableClaim> claims, List<ReadModelTableMetadataScope> unknown)
    {
        if (scope.IsUnknown)
        {
            unknown.Add(scope);
            return;
        }

        try
        {
            await ReadDefinitions(scope, claims);
            await ReadHistory(scope, claims);
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            // A scope that cannot be read is a possible owner, never an empty one.
            unknown.Add(ReadModelTableMetadataScope.Unknown(scope.EventStoreId, scope.Namespace, "metadata", error.GetType().Name));
        }
    }

    async Task ReadDefinitions(ReadModelTableMetadataScope scope, List<ReadModelTableClaim> claims)
    {
        if (scope.Definitions is null)
        {
            return;
        }

        var definitions = await ReadTable(scope.Definitions, () => scope.Definitions.ReadModels.AsNoTracking()
            .Where(definition => definition.SinkType == WellKnownSinkTypes.SQL.Value)
            .Select(definition => new { definition.Id, definition.Name }).ToArrayAsync());
        foreach (var definition in definitions)
        {
            var owner = new ReadModelTableOwner(scope.EventStoreId, scope.Namespace!, definition.Id.Value, definition.Name.Value, ReadModelTableRole.Live, definition.Name.Value);
            claims.Add(new(owner, Registered: true));
            claims.Add(new(owner with { Role = ReadModelTableRole.Replay, LogicalName = ReadModelTableNames.Replay(owner.ContainerName) }));
        }
    }

    async Task ReadHistory(ReadModelTableMetadataScope scope, List<ReadModelTableClaim> claims)
    {
        if (scope.History is null)
        {
            return;
        }

        foreach (var replay in await ReadTable(scope.History, () => scope.History.ReplayContexts.AsNoTracking().ToArrayAsync()))
        {
            AddReplayClaims(scope, claims, replay.ReadModelIdentifier, replay.ReadModel, replay.RevertModel, occurred: false);
        }

        foreach (var occurrence in await ReadTable(scope.History, () => scope.History.ReplayedModels.AsNoTracking().ToArrayAsync()))
        {
            AddReplayClaims(scope, claims, occurrence.ReadModelIdentifier, occurrence.ReadModelName, occurrence.RevertModelName, occurred: true);
        }
    }

    /// <summary>
    /// Reads a metadata table, treating a missing table as empty only when that negative is positively visible.
    /// </summary>
    /// <typeparam name="T">The row type.</typeparam>
    /// <param name="metadata">The metadata context that is read.</param>
    /// <param name="read">The read to run.</param>
    /// <returns>The rows, or none when the table is positively absent.</returns>
    async Task<T[]> ReadTable<T>(DbContext metadata, Func<Task<T[]>> read)
    {
        try
        {
            return await read();
        }
        catch (Exception error) when (SqlErrors.IsMissingTable(error))
        {
            if (await MissingIsVisible(metadata))
            {
                return [];
            }

            throw;
        }
    }

    async Task<bool> MissingIsVisible(DbContext metadata)
    {
        if (catalog.Provider != DatabaseType.SqlServer)
        {
            // SQLite opened the file; PostgreSQL's undefined-table code does not depend on privileges.
            return true;
        }

        var visible = await metadata.Database
            .SqlQueryRaw<int?>("SELECT HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'VIEW DEFINITION') AS [Value]")
            .SingleAsync();
        return visible == 1;
    }
}
