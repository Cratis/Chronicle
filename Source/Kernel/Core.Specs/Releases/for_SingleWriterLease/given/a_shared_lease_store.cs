// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clustering;
using Cratis.Chronicle.Storage.SingleWriter;

namespace Cratis.Chronicle.SingleWriter.for_SingleWriterLease.given;

/// <summary>
/// An atomic, in-process lease store shared by several keepers, standing in for one kernel database.
/// </summary>
public class a_shared_lease_store : ISingleWriterLeaseStorage
{
    readonly object _gate = new();
    SingleWriterLeaseOwner? _owner;

    public Task<SingleWriterLeaseAttempt> TryAcquire(SingleWriterLeaseClaim claim, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_owner is null || _owner == claim.Owner)
            {
                _owner = claim.Owner;
                return Task.FromResult(new SingleWriterLeaseAttempt(true, null));
            }

            return Task.FromResult(new SingleWriterLeaseAttempt(false, new(_owner, "holder", "1", DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.Add(claim.Ttl))));
        }
    }

    public Task<bool> Renew(SingleWriterLeaseOwner owner, TimeSpan ttl, CancellationToken cancellationToken)
    {
        lock (_gate) return Task.FromResult(_owner == owner);
    }

    public Task Release(SingleWriterLeaseOwner owner, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_owner == owner) _owner = null;
        }

        return Task.CompletedTask;
    }
}
