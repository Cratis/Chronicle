// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

/// <summary>
/// Resolves ownership before any dynamic user-table access. It never provisions a user table.
/// Reservations must be provisioned and marked Ready by their owner under the same schema lock.
/// </summary>
/// <param name="bindings">The durable metadata-only ledger.</param>
/// <param name="catalog">The catalog for the resolved database and schema.</param>
/// <param name="ownership">Persisted, scoped ownership evidence.</param>
internal sealed class ReadModelTableNameResolver(
    IReadModelTableNameBindings bindings,
    IReadModelTableCatalog catalog,
    IReadModelTableOwnership ownership)
{
    readonly ReadModelTableOwnershipConflicts _conflicts = new(catalog);

    /// <summary>
    /// Resolves a full logical identity, committing only a proven adoption or a new reservation.
    /// A require-existing request cannot create either a reservation or a user table.
    /// </summary>
    /// <param name="owner">The complete logical identity, including event store and namespace.</param>
    /// <param name="requireExisting">Whether this is a read/removal of existing state.</param>
    /// <returns>The committed immutable assignment and its current provision state.</returns>
    /// <exception cref="ReadModelTableNameBindingFailure">The ledger name is occupied by incompatible metadata.</exception>
    internal async Task<ReadModelTableNameBinding> Resolve(ReadModelTableOwner owner, bool requireExisting = false)
    {
        try
        {
            return await bindings.UnderLock(() => ResolveUnderLock(owner, requireExisting));
        }
        catch (ReadModelTableBindingsSchemaMismatch)
        {
            throw new ReadModelTableNameBindingFailure(owner, ReadModelTableNames.Candidate(catalog.Provider, owner.LogicalName), "internal ledger name is occupied by incompatible metadata");
        }
    }

    static ReadModelTableNameBinding Adopt(ReadModelTableOwner owner, string physical, IReadOnlyList<ReadModelTableClaim> ownClaims)
    {
        var proved = owner.Role switch
        {
            ReadModelTableRole.Live => ownClaims.Any(claim => claim.Registered),
            ReadModelTableRole.Backup => ownClaims.Any(claim => claim.Occurred || claim.PromotedPhysicalName is not null),
            _ => false
        };
        if (!proved)
        {
            throw new ReadModelTableNameBindingFailure(owner, physical, "unowned relation or interrupted legacy replay; recovery required");
        }
        return ReadModelTableNameBinding.For(owner, physical, ReadModelTableBindingState.Ready);
    }

    async Task<ReadModelTableNameBinding> ResolveUnderLock(ReadModelTableOwner owner, bool requireExisting)
    {
        var bound = await bindings.Find(owner.Id);
        if (bound is not null)
        {
            return await Validate(owner, bound, requireExisting);
        }

        var read = await ownership.Read();
        var claims = read.Known;
        var existingBindings = await bindings.Read();
        var candidate = ReadModelTableNames.Candidate(catalog.Provider, owner.LogicalName);
        var legacy = ReadModelTableNames.Legacy(catalog.Provider, owner.LogicalName);
        await _conflicts.Check(owner, candidate, claims, existingBindings);
        var selected = await Select(owner, candidate, legacy, claims, requireExisting);
        await _conflicts.Check(owner, selected.PhysicalName, claims, existingBindings);
        if (read.Unknown.Count > 0 && selected.State != ReadModelTableBindingState.Ready)
        {
            // A possible owner we cannot read: reserve and create nothing. Adopting an already existing relation
            // on the owner's own proof is what the base did, so it stays available.
            throw new ReadModelTableMetadataUnavailable(owner, read.Unknown);
        }
        if (selected.State == ReadModelTableBindingState.Ready && !await ownership.HasExpectedShape(selected.PhysicalName))
        {
            throw new ReadModelTableNameBindingFailure(owner, selected.PhysicalName, "existing relation has an unexpected read-model shape");
        }
        if (await bindings.TryInsert(selected))
        {
            return selected;
        }

        // A unique conflict is not success: another resolver may have claimed this physical name.
        var winner = await bindings.Find(owner.Id);
        if (winner is not null)
        {
            return await Validate(owner, winner, requireExisting);
        }
        foreach (var binding in await bindings.Read())
        {
            if (await catalog.Equal(binding.PhysicalName, selected.PhysicalName))
            {
                throw new ReadModelTableNameBindingFailure(owner, selected.PhysicalName, "another owner won the reservation", binding.Owner);
            }
        }
        throw new ReadModelTableNameBindingFailure(owner, selected.PhysicalName, "reservation conflict has no attributable winner; recovery required");
    }

    async Task<ReadModelTableNameBinding> Validate(ReadModelTableOwner owner, ReadModelTableNameBinding binding, bool requireExisting)
    {
        if (binding.Id != owner.Id || binding.Owner != owner)
        {
            throw new ReadModelTableNameBindingFailure(owner, binding.PhysicalName, "stored full identity does not match lookup key", binding.Owner);
        }
        if (ReadModelTableNames.Candidate(catalog.Provider, binding.PhysicalName) != binding.PhysicalName || string.IsNullOrEmpty(binding.PhysicalName))
        {
            throw new ReadModelTableNameBindingFailure(owner, binding.PhysicalName, "stored identifier exceeds provider limit or is empty");
        }
        if (!Enum.IsDefined(binding.State) || binding.State == ReadModelTableBindingState.Removed)
        {
            throw new ReadModelTableNameBindingFailure(owner, binding.PhysicalName, "removed or invalid binding requires explicit owner recovery");
        }
        if (binding.State == ReadModelTableBindingState.Reserved)
        {
            // Finishing a reservation creates a table, so it needs the same proof as a new reservation.
            // A Ready binding was proved when it committed and the unique physical key keeps it, so it needs no scan.
            var read = await ownership.Read();
            if (read.Unknown.Count > 0)
            {
                throw new ReadModelTableMetadataUnavailable(owner, read.Unknown);
            }

            await _conflicts.Check(owner, binding.PhysicalName, read.Known, await bindings.Read());
        }

        var physical = await catalog.Find(binding.PhysicalName);
        if (binding.State == ReadModelTableBindingState.Ready && physical is null)
        {
            throw new ReadModelTableNameBindingFailure(owner, binding.PhysicalName, "Ready table is unexpectedly missing; recovery required");
        }
        if (requireExisting && binding.State != ReadModelTableBindingState.Ready)
        {
            throw new ReadModelTableNameBindingFailure(owner, binding.PhysicalName, "table is not Ready");
        }
        if (physical is not null && physical != binding.PhysicalName)
        {
            throw new ReadModelTableNameBindingFailure(owner, binding.PhysicalName, "catalog spelling conflicts with immutable binding");
        }
        return binding;
    }

    async Task<ReadModelTableNameBinding> Select(
        ReadModelTableOwner owner,
        string candidate,
        string legacy,
        IReadOnlyList<ReadModelTableClaim> claims,
        bool requireExisting)
    {
        var ownClaims = claims.Where(claim => claim.Owner == owner).ToArray();
        var promoted = ownClaims.Where(claim => claim.PromotedPhysicalName is not null).Select(claim => claim.PromotedPhysicalName!).Distinct(StringComparer.Ordinal).ToArray();
        if (promoted.Length > 1 || ownClaims.Any(claim => claim.Displaced))
        {
            throw new ReadModelTableNameBindingFailure(owner, candidate, "ambiguous or displaced promotion ownership");
        }
        var candidateTable = await catalog.Find(candidate);
        var legacyTable = legacy == candidate ? candidateTable : await catalog.Find(legacy);
        if (candidateTable is not null && legacyTable is not null && !await catalog.Equal(candidateTable, legacyTable))
        {
            throw new ReadModelTableNameBindingFailure(owner, candidate, "both legacy and candidate tables exist without a binding");
        }
        if (promoted.Length == 0 && candidateTable is not null && legacyTable is null && candidate != legacy)
        {
            throw new ReadModelTableNameBindingFailure(owner, candidate, "unbound hashed relation has no ownership proof");
        }
        var existing = promoted.Length == 1 ? await catalog.Find(promoted[0]) : legacyTable ?? candidateTable;
        if (promoted.Length == 1 && existing is null)
        {
            throw new ReadModelTableNameBindingFailure(owner, promoted[0], "promotion-owned backup is missing");
        }
        if (existing is not null)
        {
            return Adopt(owner, existing, ownClaims);
        }
        if (requireExisting)
        {
            throw new ReadModelTableNameBindingFailure(owner, candidate, "required table is missing");
        }
        return ReadModelTableNameBinding.For(owner, candidate, ReadModelTableBindingState.Reserved);
    }
}
