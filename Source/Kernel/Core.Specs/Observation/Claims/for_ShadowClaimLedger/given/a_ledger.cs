// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation.Claims.for_ShadowClaimLedger.given;

public class a_ledger : Specification
{
    internal ShadowClaimLedger _ledger;
    protected Key _partition;
    protected ISet<Key> _replaying;
    protected ISet<Key> _catchingUp;
    protected EventSequenceNumber _resume;
    internal IReadOnlyList<(Key? Partition, ShadowClaimDivergence Divergence)> _divergences;

    void Establish()
    {
        _ledger = ShadowClaimLedger.Restore(null, null, null, out _);
        _partition = "partition";
        _replaying = new HashSet<Key>();
        _catchingUp = new HashSet<Key>();
        _resume = 42;
    }

    protected PartitionClaim Claim() => _ledger.Snapshot().Claims.Single();
    protected void GrantReplay() => _ledger.Grant(_partition, ClaimHolderKind.PartitionReplay, null, _resume);
}
