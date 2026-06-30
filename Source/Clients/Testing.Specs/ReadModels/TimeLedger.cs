// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model with a child collection keyed by a <see cref="TimeOnly"/> payload value (children on the
/// same event source as the parent), used to verify the in-memory harness materializes
/// <see cref="TimeOnly"/>-keyed child collections.
/// </summary>
/// <param name="Id">Ledger identifier.</param>
/// <param name="Name">The ledger name.</param>
/// <param name="Slots">Slot lines keyed by <see cref="SlotAmountRecorded.Slot"/> (a <see cref="TimeOnly"/>).</param>
[Passive]
[FromEvent<LedgerOpened>]
public record TimeLedger(
    Guid Id,
    string Name,

    [ChildrenFrom<SlotAmountRecorded>(key: nameof(SlotAmountRecorded.Slot))]
    IEnumerable<SlotLine> Slots);
