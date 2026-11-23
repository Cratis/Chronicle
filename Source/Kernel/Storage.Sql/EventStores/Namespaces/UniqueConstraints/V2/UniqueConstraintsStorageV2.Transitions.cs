// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Monads;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.V2;

public sealed partial class UniqueConstraintsStorageV2
{
    /// <inheritdoc/>
    public async Task<Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>> TryTransition(ConstraintName constraint, UniqueConstraintWriteAuthority expected, UniqueConstraintWritePhase phase, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateAuthority(expected);
        if (phase is null) throw new InvalidUniqueConstraintV2Input(nameof(UniqueConstraintWritePhase));
        var failure = await CheckAuthority(constraint, expected, cancellationToken);
        if (failure is not null) return Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>.Failed(failure.Value);
        await using var context = _schema.Context();
        await using var transaction = await V2Transactions.Begin(context, cancellationToken);
        failure = await Fence(context, constraint, expected, cancellationToken);
        if (failure is not null) return Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>.Failed(failure.Value);
        var next = expected.Revision.Next();
        if (!next.IsSuccess) return Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>.Failed(next.AsT1);
        var live = phase.RunId is null;
        var run = phase.RunId?.Value;
        await Matching(context, constraint, expected).ExecuteUpdateAsync(
            setters => setters
                .SetProperty(row => row.IsLive, live)
                .SetProperty(row => row.RunId, run)
                .SetProperty(row => row.Revision, next.AsT0.Value),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>.Success(expected with { Phase = phase, Revision = next.AsT0 });
    }
}
