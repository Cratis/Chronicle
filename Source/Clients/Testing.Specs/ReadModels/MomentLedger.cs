// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model with a child collection keyed by a <see cref="DateTimeOffset"/> payload value (children on
/// the same event source as the parent), used to verify the in-memory harness materializes
/// <see cref="DateTimeOffset"/>-keyed child collections.
/// </summary>
/// <param name="Id">Ledger identifier.</param>
/// <param name="Name">The ledger name.</param>
/// <param name="Moments">Moment lines keyed by <see cref="MomentAmountRecorded.Moment"/> (a <see cref="DateTimeOffset"/>).</param>
[Passive]
[FromEvent<LedgerOpened>]
public record MomentLedger(
    Guid Id,
    string Name,

    [ChildrenFrom<MomentAmountRecorded>(key: nameof(MomentAmountRecorded.Moment))]
    IEnumerable<MomentLine> Moments);
