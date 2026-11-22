// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Monads;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.V2;

public sealed partial class UniqueConstraintsStorageV2
{
    /// <inheritdoc/>
    public async Task<Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>> TryTransition(ConstraintName constraint, UniqueConstraintWriteAuthority expected, UniqueConstraintWritePhase phase, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (constraint is null) throw new InvalidUniqueConstraintV2Input(nameof(constraint));
        ValidateText(constraint.Value);
        ValidateAuthority(expected);
        ValidatePhase(phase);
        var failure = await CheckAuthority(constraint, expected, cancellationToken);
        if (failure is not null) return Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>.Failed(failure.Value);
        var revision = expected.Revision.Next();
        if (!revision.IsSuccess) return Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>.Failed(revision.AsT1);
        await V2Topology.Require(_database, cancellationToken);
        var next = expected with { Phase = phase, Revision = revision.AsT0 };
        var fields = new BsonDocument { { "Run", V2Documents.Run(phase) }, { "Revision", next.Revision.Value }, { "Fence", Guid.NewGuid().ToString("N") } };

        // A single conditional update waits behind a fenced writer without consuming a transaction retry budget.
        var updated = await _database.GetCollection<BsonDocument>(V2Schema.Authorities).UpdateOneAsync(
            V2Documents.Proof(V2Documents.Identity(_sequence, constraint.Value), expected),
            new BsonDocument("$set", fields),
            new UpdateOptions { Collation = V2Schema.Simple },
            cancellationToken);
        if (updated.MatchedCount == 1) return Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>.Success(next);
        failure = await CheckAuthority(constraint, expected, cancellationToken);
        return Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>.Failed(failure ?? UniqueConstraintWriteFailure.AuthorityMismatch);
    }
}
