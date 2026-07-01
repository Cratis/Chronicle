// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model with a child collection keyed by a <see cref="DateTime"/> payload value (children on the
/// same event source as the parent), used to verify the in-memory harness materializes
/// <see cref="DateTime"/>-keyed child collections.
/// </summary>
/// <param name="Id">Ledger identifier.</param>
/// <param name="Name">The ledger name.</param>
/// <param name="Stamps">Stamp lines keyed by <see cref="StampAmountRecorded.Stamp"/> (a <see cref="DateTime"/>).</param>
[Passive]
[FromEvent<LedgerOpened>]
public record StampLedger(
    Guid Id,
    string Name,

    [ChildrenFrom<StampAmountRecorded>(key: nameof(StampAmountRecorded.Stamp))]
    IEnumerable<StampLine> Stamps);
