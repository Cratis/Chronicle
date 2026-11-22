// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.V2;

public sealed partial class UniqueConstraintsStorageV2
{
    static async Task<UniqueConstraintWriteFailure?> SaveClaim(V2DbContext context, UniqueConstraintMode mode, V2Claim claim, CancellationToken cancellationToken)
    {
        var claimedValue = context.Claims.Where(row => row.Scope == claim.Scope && row.Value == claim.Value);
        if (await claimedValue.AnyAsync(cancellationToken))
        {
            if (!await claimedValue.AnyAsync(row => row.EventSourceId == claim.EventSourceId, cancellationToken)) return UniqueConstraintWriteFailure.ConflictingClaim;

            // A repeat changes only subject metadata; the original sequence belongs to the claim.
            await claimedValue.ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Subject, claim.Subject), cancellationToken);
            return null;
        }

        if (mode == UniqueConstraintMode.PerEventSource)
        {
            await context.Claims.Where(row => row.Scope == claim.Scope && row.EventSourceId == claim.EventSourceId).ExecuteDeleteAsync(cancellationToken);
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
