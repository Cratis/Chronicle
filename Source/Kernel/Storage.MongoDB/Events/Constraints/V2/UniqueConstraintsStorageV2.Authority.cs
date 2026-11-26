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
    public async Task<Option<UniqueConstraintWriteAuthority>> GetAuthority(ConstraintName constraint, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (constraint is null) throw new InvalidUniqueConstraintV2Input(nameof(constraint));
        ValidateText(constraint.Value);
        if (!await _schema.EnsureMetadata(false, cancellationToken)) return Option<UniqueConstraintWriteAuthority>.None();
        var identity = V2Documents.Identity(_sequence, constraint.Value);
        var document = await _database.GetCollection<BsonDocument>(V2Schema.Authorities)
            .Find(new BsonDocument("_id", identity["_id"]), new FindOptions { Collation = V2Schema.Simple }).SingleOrDefaultAsync(cancellationToken);
        return document is null ? Option<UniqueConstraintWriteAuthority>.None() : V2Documents.Authority(document, identity);
    }

    /// <inheritdoc/>
    public async Task<Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>> InitializeAuthority(ConstraintName constraint, UniqueConstraintWriteAuthority authority, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (constraint is null) throw new InvalidUniqueConstraintV2Input(nameof(constraint));
        ValidateText(constraint.Value);
        ValidateAuthority(authority);
        await V2Topology.Require(_database, cancellationToken);
        await _schema.EnsureMetadata(true, cancellationToken);
        var document = V2Documents.Proof(V2Documents.Identity(_sequence, constraint.Value), authority);
        document.Add("Fence", Guid.NewGuid().ToString("N"));
        try
        {
            await _database.GetCollection<BsonDocument>(V2Schema.Authorities).InsertOneAsync(document, cancellationToken: cancellationToken);
            return Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>.Success(authority);
        }
        catch (MongoException error) when (V2Transactions.IsDuplicate(error))
        {
            // Validate complete logical ownership before treating a duplicate initializer as a mismatch.
            await GetAuthority(constraint, cancellationToken);
            return Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>.Failed(UniqueConstraintWriteFailure.AuthorityMismatch);
        }
    }

    async Task<UniqueConstraintWriteFailure?> CheckAuthority(ConstraintName constraint, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken)
    {
        var authority = await GetAuthority(constraint, cancellationToken);
        if (!authority.HasValue) return UniqueConstraintWriteFailure.MissingAuthority;
        return (UniqueConstraintWriteAuthority)authority.Value == expected ? null : UniqueConstraintWriteFailure.AuthorityMismatch;
    }

    async Task<UniqueConstraintWriteFailure?> Fence(IClientSessionHandle session, V2Binding binding, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken)
    {
        var identity = V2Documents.Identity(binding.Sequence, binding.Constraint);
        var collection = _database.GetCollection<BsonDocument>(V2Schema.Authorities);
        var document = await collection.Find(session, new BsonDocument("_id", identity["_id"]), new FindOptions { Collation = V2Schema.Simple }).SingleOrDefaultAsync(cancellationToken);
        if (document is null) return UniqueConstraintWriteFailure.MissingAuthority;
        if (V2Documents.Authority(document, identity) != expected) return UniqueConstraintWriteFailure.AuthorityMismatch;
        var updated = await collection.UpdateOneAsync(
            session,
            V2Documents.Proof(identity, expected),
            new BsonDocument("$set", new BsonDocument("Fence", Guid.NewGuid().ToString("N"))),
            new UpdateOptions { Collation = V2Schema.Simple },
            cancellationToken);
        return updated.MatchedCount == 1 ? null : UniqueConstraintWriteFailure.AuthorityMismatch;
    }
}
