// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints.V2;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints;

public partial class UniqueConstraintsStorageV2
{
    /// <inheritdoc/>
    public Task<Option<UniqueConstraintWriteAuthority>> GetAuthority(ConstraintName constraint, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_authorities.TryGetValue(constraint, out var authority)
                ? (Option<UniqueConstraintWriteAuthority>)authority
                : Option<UniqueConstraintWriteAuthority>.None());
        }
    }

    /// <inheritdoc/>
    public Task<Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>> InitializeAuthority(ConstraintName constraint, UniqueConstraintWriteAuthority authority, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_authorities.TryAdd(constraint, authority))
            {
                return Task.FromResult(Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>.Failed(UniqueConstraintWriteFailure.AuthorityMismatch));
            }

            _claims.Add(constraint, new());
            return Task.FromResult(Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>.Success(authority));
        }
    }

    /// <inheritdoc/>
    public Task<Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>> TryTransition(ConstraintName constraint, UniqueConstraintWriteAuthority expected, UniqueConstraintWritePhase phase, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            var failure = CheckAuthority(constraint, expected, cancellationToken);
            if (failure is not null)
            {
                return Task.FromResult(Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>.Failed(failure.Value));
            }

            var next = expected.Revision.Next();
            if (!next.IsSuccess)
            {
                return Task.FromResult(Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>.Failed(next.AsT1));
            }

            var authority = expected with { Phase = phase, Revision = next.AsT0 };
            _authorities[constraint] = authority;
            return Task.FromResult(Result<UniqueConstraintWriteAuthority, UniqueConstraintWriteFailure>.Success(authority));
        }
    }

    UniqueConstraintWriteFailure? CheckAuthority(ConstraintName constraint, UniqueConstraintWriteAuthority expected, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_authorities.TryGetValue(constraint, out var actual))
        {
            return UniqueConstraintWriteFailure.MissingAuthority;
        }

        return actual == expected ? null : UniqueConstraintWriteFailure.AuthorityMismatch;
    }
}
