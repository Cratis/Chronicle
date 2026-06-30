// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model with a child collection keyed by an <see cref="int"/> payload value (children on the same
/// event source as the parent), used to verify the in-memory harness materializes <see cref="int"/>-keyed
/// child collections.
/// </summary>
/// <param name="Id">Ledger identifier.</param>
/// <param name="Name">The ledger name.</param>
/// <param name="Buckets">Bucket lines keyed by <see cref="BucketAmountRecorded.Bucket"/> (an <see cref="int"/>).</param>
[Passive]
[FromEvent<LedgerOpened>]
public record BucketLedger(
    Guid Id,
    string Name,

    [ChildrenFrom<BucketAmountRecorded>(key: nameof(BucketAmountRecorded.Bucket))]
    IEnumerable<BucketLine> Buckets);
