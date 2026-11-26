// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.V2;

public sealed partial class UniqueConstraintsStorageV2
{
    static async Task<UniqueConstraintWriteFailure?> SaveClaim(V2DbContext context, UniqueConstraintMode mode, V2Claim claim, CancellationToken cancellationToken)
    {
        var claimedValue = context.Claims.Where(row => row.Scope == claim.Scope && row.Value == claim.Value);
        if (await claimedValue.AnyAsync(cancellationToken))
        {
            var ownedValue = claimedValue.WhereExact(context, row => row.EventSourceId, claim.EventSourceId);
            if (!await ownedValue.AnyAsync(cancellationToken)) return UniqueConstraintWriteFailure.ConflictingClaim;

            // Only an exact-byte owner repeat changes subject metadata; the first sequence belongs to the claim.
            await ownedValue.ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Subject, claim.Subject), cancellationToken);
            return null;
        }

        if (mode == UniqueConstraintMode.PerEventSource)
        {
            await context.Claims.Where(row => row.Scope == claim.Scope)
                .WhereExact(context, row => row.EventSourceId, claim.EventSourceId).ExecuteDeleteAsync(cancellationToken);
        }

        context.Claims.Add(claim);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            // The caller rolls back the whole transaction, including any replaced owner claim.
            return UniqueConstraintWriteFailure.ConflictingClaim;
        }
    }
}
