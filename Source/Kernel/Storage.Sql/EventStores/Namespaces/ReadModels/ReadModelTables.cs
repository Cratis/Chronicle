// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Arc.Connections;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Represents an implementation of <see cref="IReadModelTables"/> that resolves each logical table to its bound physical name once.
/// </summary>
/// <param name="placements">The <see cref="IReadModelTablePlacements"/> that say where tables live.</param>
internal sealed class ReadModelTables(IReadModelTablePlacements placements) : IReadModelTables
{
    readonly ConcurrentDictionary<(ReadModelTableHandle Handle, ReadModelTablePlacement Placement), string> _resolved = new();

    /// <inheritdoc/>
    public async Task<DbContextScope<ReadModelDbContext>> Open(ReadModelTableHandle handle, IReadOnlyList<ProjectedColumn> columns, ConnectionPurpose purpose = ConnectionPurpose.Runtime)
    {
        var place = await placements.Locate(handle.EventStore, handle.Namespace, purpose);
        var placement = place.Placement;
        if (placement is not null && _resolved.TryGetValue((handle, placement.Value), out var known))
        {
            try
            {
                return Scope(await place.OpenTable(known, columns, requireExisting: handle.Role != ReadModelTableRole.Replay));
            }
            catch (Exception error) when (IsMissing(error))
            {
                _resolved.TryRemove((handle, placement.Value), out _);
            }
        }

        var physical = await Provision(handle, columns, place);
        var context = await place.OpenTable(physical, columns, requireExisting: handle.Role != ReadModelTableRole.Replay);
        if (placement is not null)
        {
            _resolved[(handle, placement.Value)] = physical;
        }

        return Scope(context);
    }

    /// <inheritdoc/>
    public async Task<ReadModelTableNameBinding> Reserve(ReadModelTableHandle handle)
    {
        var place = await placements.Locate(handle.EventStore, handle.Namespace, ConnectionPurpose.Schema);
        await using var ledger = await place.OpenLedger();
        var binding = await Resolver(place, ledger).Resolve(handle.Owner, requireExisting: false);
        if (IsReservedBackup(handle, binding) && await new ReadModelTableCatalog(ledger, place.Schema).Find(binding.PhysicalName) is not null)
        {
            throw new ReadModelTableNameBindingFailure(handle.Owner, binding.PhysicalName, "a reserved backup already exists without completed promotion evidence");
        }
        return binding;
    }

    /// <inheritdoc/>
    public async Task<ReadModelTableNameBinding?> Find(ReadModelTableHandle handle)
    {
        var place = await placements.Locate(handle.EventStore, handle.Namespace, ConnectionPurpose.Schema);
        await using var ledger = await place.OpenLedger();
        try
        {
            return await new ReadModelTableNameBindings(ledger, place.Schema, place.LedgerMigrator).Find(handle.Id);
        }
        catch (Exception error) when (SqlErrors.IsMissingTable(error))
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<ReadModelTableNameBinding?> FindBackup(ReadModelTableHandle handle)
    {
        var place = await placements.Locate(handle.EventStore, handle.Namespace, ConnectionPurpose.Schema);
        await using var ledger = await place.OpenLedger();
        var bindings = await new ReadModelTableNameBindings(ledger, place.Schema, place.LedgerMigrator).Read();
        var matches = bindings.Where(binding => SameBackupOccurrence(handle, binding.Owner)).ToArray();
        if (matches.Length > 1)
        {
            throw new ReadModelTableNameBindingFailure(handle.Owner, handle.Owner.LogicalName, "several bindings claim this backup occurrence");
        }

        return matches.SingleOrDefault() ?? await RecoverBackup(handle, place, ledger);
    }

    /// <inheritdoc/>
    public Task<bool> Transition(ReadModelTableHandle handle, ReadModelTableBindingState expected, ReadModelTableBindingState next, DbContext? enlist = null)
    {
        _resolved.Clear();
        return InLedger(handle, enlist, bindings => bindings.Transition(handle.Id, expected, next));
    }

    /// <inheritdoc/>
    public Task<bool> SetReplayAttempt(ReadModelTableHandle handle, string? expected, string? attempt, DbContext? enlist = null) =>
        InLedger(handle, enlist, bindings => bindings.SetReplayAttempt(handle.Id, expected, attempt));

    /// <inheritdoc/>
    public void Clear() => _resolved.Clear();

    static ReadModelTableOwnership Ownership(ReadModelTablePlace place, ReadModelTableBindingsDbContext ledger)
    {
        var catalog = new ReadModelTableCatalog(ledger, place.Schema);
        return new(place.Scopes, ledger, catalog, catalog.HasPrimaryKey);
    }

    static ReadModelTableNameResolver Resolver(ReadModelTablePlace place, ReadModelTableBindingsDbContext ledger) =>
        new(new ReadModelTableNameBindings(ledger, place.Schema, place.LedgerMigrator), new ReadModelTableCatalog(ledger, place.Schema), Ownership(place, ledger));

    static DbContextScope<ReadModelDbContext> Scope(ReadModelDbContext context) => new(context, static () => { });

    static bool IsMissing(Exception error) => error is ReadModelTableMissing || error.InnerException is ReadModelTableMissing || SqlErrors.IsMissingTable(error);

    static bool IsReservedBackup(ReadModelTableHandle handle, ReadModelTableNameBinding binding) =>
        handle.Owner.Role == ReadModelTableRole.Backup && binding.State == ReadModelTableBindingState.Reserved;

    static bool SameBackupOccurrence(ReadModelTableHandle handle, ReadModelTableOwner owner) =>
        owner.EventStoreId == handle.EventStore && owner.Namespace == handle.Namespace &&
        owner.ReadModelIdentifier == handle.Owner.ReadModelIdentifier && owner.Role == ReadModelTableRole.Backup &&
        owner.LogicalName == handle.Owner.LogicalName;

    static async Task<ReadModelTableNameBinding?> RecoverBackup(ReadModelTableHandle handle, ReadModelTablePlace place, ReadModelTableBindingsDbContext ledger)
    {
        var claims = await Ownership(place, ledger).Read();
        if (claims.Unknown.Count > 0)
        {
            throw new ReadModelTableMetadataUnavailable(handle.Owner, claims.Unknown);
        }

        var owners = claims.Known.Where(claim => SameBackupOccurrence(handle, claim.Owner) &&
            (claim.Occurred || claim.PromotedPhysicalName is not null))
            .Select(claim => claim.Owner).Distinct().ToArray();
        if (owners.Length > 1)
        {
            throw new ReadModelTableNameBindingFailure(handle.Owner, handle.Owner.LogicalName, "several historical owners claim this backup occurrence");
        }

        return owners.Length == 0 ? null : await Resolver(place, ledger).Resolve(owners[0], requireExisting: true);
    }

    async Task<bool> InLedger(ReadModelTableHandle handle, DbContext? enlist, Func<ReadModelTableNameBindings, Task<bool>> change)
    {
        var place = await placements.Locate(handle.EventStore, handle.Namespace, ConnectionPurpose.Schema);
        if (enlist is null)
        {
            await using var ledger = await place.OpenLedger();
            return await change(new ReadModelTableNameBindings(ledger, place.Schema, place.LedgerMigrator));
        }

        await using var enlisted = await ReadModelTableBindingsDbContext.Enlist(enlist, place.Schema);
        return await change(new ReadModelTableNameBindings(enlisted, place.Schema, place.LedgerMigrator));
    }

    async Task<string> Provision(ReadModelTableHandle handle, IReadOnlyList<ProjectedColumn> columns, ReadModelTablePlace place)
    {
        await using var ledger = await place.OpenLedger();
        var bindings = new ReadModelTableNameBindings(ledger, place.Schema, place.LedgerMigrator);
        var binding = await Resolver(place, ledger).Resolve(handle.Owner, requireExisting: handle.Role == ReadModelTableRole.Backup);
        if (binding.State != ReadModelTableBindingState.Reserved)
        {
            return binding.PhysicalName;
        }

        if (handle.Role == ReadModelTableRole.Backup)
        {
            throw new ReadModelTableNameBindingFailure(handle.Owner, binding.PhysicalName, "a backup table is created by the replay promotion, never on demand");
        }

        await using (await place.OpenTable(binding.PhysicalName, columns, requireExisting: false))
        {
        }

        if (!await bindings.Transition(handle.Id, ReadModelTableBindingState.Reserved, ReadModelTableBindingState.Ready)
            && (await bindings.Find(handle.Id))?.State != ReadModelTableBindingState.Ready)
        {
            throw new ReadModelTableNameBindingFailure(handle.Owner, binding.PhysicalName, "the binding changed while its table was being created");
        }

        return binding.PhysicalName;
    }
}
