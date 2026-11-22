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
    public async Task<Option<UniqueConstraintWriteAuthority>> GetAuthority(ConstraintName constraint, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var row = await ReadAuthority(constraint, cancellationToken);
        return row is null ? Option<UniqueConstraintWriteAuthority>.None() : (Option<UniqueConstraintWriteAuthority>)Proof(row);
    }

    /// <inheritdoc/>
    public async Task<Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>> InitializeAuthority(ConstraintName constraint, UniqueConstraintWriteAuthority authority, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateAuthority(authority);
        await _schema.EnsureMetadata(cancellationToken);
        await using var context = _schema.Context();
        context.Authorities.Add(new(AuthorityId(constraint), eventSequenceId.Value, constraint.Value, authority.Epoch.Value, authority.Phase.RunId is null, authority.Phase.RunId?.Value, authority.Revision.Value));
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>.Success(authority);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            // Verify the full logical tuple before classifying a digest collision as a lost initializer race.
            await ReadAuthority(constraint, cancellationToken);
            return Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>.Failed(UniqueConstraintWriteFailure.AuthorityMismatch);
        }
    }

    static UniqueConstraintWriteAuthority Proof(V2Authority row) => new(new(row.Epoch), row.IsLive ? UniqueConstraintWritePhase.Live : UniqueConstraintWritePhase.Run(new(row.RunId!.Value)), new(row.Revision));

    string AuthorityId(ConstraintName constraint) => V2Identity.Authority(eventSequenceId.Value, constraint.Value);

    async Task<V2Authority?> ReadAuthority(ConstraintName constraint, CancellationToken cancellationToken)
    {
        await using var context = _schema.Context();
        if (!await V2Schema.Exists(context, V2Identity.Authorities)) return null;
        var id = AuthorityId(constraint);
        var row = await context.Authorities.AsNoTracking().SingleOrDefaultAsync(row => row.Id == id, cancellationToken);
        if (row is null) return null;
        if ((row.Sequence, row.Constraint) != (eventSequenceId.Value, constraint.Value)) throw new UniqueConstraintV2SchemaMismatch();
        return row;
    }

    async Task<UniqueConstraintWriteFailure?> CheckAuthority(ConstraintName constraint, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken)
    {
        var current = await ReadAuthority(constraint, cancellationToken);
        if (current is null) return UniqueConstraintWriteFailure.MissingAuthority;
        return Proof(current) == expected ? null : UniqueConstraintWriteFailure.AuthorityMismatch;
    }

    IQueryable<V2Authority> Matching(V2DbContext context, ConstraintName constraint, UniqueConstraintWriteAuthority expected)
    {
        var id = AuthorityId(constraint);
        var live = expected.Phase.RunId is null;
        var run = expected.Phase.RunId?.Value;
        return context.Authorities.Where(row => row.Id == id && row.Epoch == expected.Epoch.Value && row.IsLive == live && row.RunId == run && row.Revision == expected.Revision.Value);
    }

    async Task<UniqueConstraintWriteFailure?> Fence(V2DbContext context, ConstraintName constraint, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken)
    {
        // Change a stored value to acquire the writer lock, then restore the reusable proof inside this transaction.
        // At the revision ceiling, move down temporarily instead of overflowing. No intermediate proof is committed.
        var fencedRevision = expected.Revision.Value == long.MaxValue ? expected.Revision.Value - 1 : expected.Revision.Value + 1;
        var matched = await Matching(context, constraint, expected).ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Revision, fencedRevision), cancellationToken);
        if (matched == 1)
        {
            var fencedProof = expected with { Revision = new(fencedRevision) };
            var restored = await Matching(context, constraint, fencedProof).ExecuteUpdateAsync(setters => setters.SetProperty(row => row.Revision, expected.Revision.Value), cancellationToken);
            if (restored == 1) return null;
            return UniqueConstraintWriteFailure.AuthorityMismatch;
        }

        var id = AuthorityId(constraint);
        return await context.Authorities.AnyAsync(row => row.Id == id, cancellationToken)
            ? UniqueConstraintWriteFailure.AuthorityMismatch
            : UniqueConstraintWriteFailure.MissingAuthority;
    }
}
