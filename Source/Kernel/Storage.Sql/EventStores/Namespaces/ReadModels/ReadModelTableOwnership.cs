// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Reads existing definitions and replay history without migrating or opening a dynamic model table.
/// Unknown placement must be included conservatively by the scope enumerator; unavailable metadata throws.
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
    public async Task<IReadOnlyList<ReadModelTableClaim>> Read()
    {
        var claims = new List<ReadModelTableClaim>();
        foreach (var scope in await scopes())
        {
            await ReadScope(scope, claims);
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
        return claims;
    }

    /// <inheritdoc/>
    public Task<bool> HasExpectedShape(string physicalName) => shape(physicalName);

    static async Task ReadScope(ReadModelTableMetadataScope scope, List<ReadModelTableClaim> claims)
    {
        var definitions = await scope.Definitions.ReadModels.AsNoTracking()
            .Where(definition => definition.SinkType == WellKnownSinkTypes.SQL.Value)
            .Select(definition => new { definition.Id, definition.Name }).ToArrayAsync();
        foreach (var definition in definitions)
        {
            var owner = new ReadModelTableOwner(scope.EventStoreId, scope.Namespace, definition.Id.Value, definition.Name.Value, ReadModelTableRole.Live, definition.Name.Value);
            claims.Add(new(owner, Registered: true));
            claims.Add(new(owner with { Role = ReadModelTableRole.Replay, LogicalName = ReadModelTableNames.Replay(owner.ContainerName) }));
        }
        foreach (var replay in await scope.History.ReplayContexts.AsNoTracking().ToArrayAsync())
        {
            AddReplayClaims(scope, claims, replay.ReadModelIdentifier, replay.ReadModel, replay.RevertModel, occurred: false);
        }
        foreach (var occurrence in await scope.History.ReplayedModels.AsNoTracking().ToArrayAsync())
        {
            AddReplayClaims(scope, claims, occurrence.ReadModelIdentifier, occurrence.ReadModelName, occurrence.RevertModelName, occurred: true);
        }
    }

    static void AddReplayClaims(ReadModelTableMetadataScope scope, List<ReadModelTableClaim> claims, string model, string container, string revert, bool occurred)
    {
        var live = new ReadModelTableOwner(scope.EventStoreId, scope.Namespace, model, container, ReadModelTableRole.Live, container);
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
}
