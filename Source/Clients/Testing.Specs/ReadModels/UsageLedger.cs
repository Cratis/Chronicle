// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model accumulating consumed capacity both at the root and broken down per period in a child
/// collection, so that the same event feeds an <see cref="AddFromAttribute{TEvent}"/> at both levels.
/// </summary>
/// <param name="Id">Ledger identifier.</param>
/// <param name="Name">The ledger name.</param>
/// <param name="TotalConsumed">The total amount consumed across every period.</param>
/// <param name="Periods">The per-period breakdown keyed by <see cref="UsageRecorded.Period"/>.</param>
[Passive]
[FromEvent<LedgerOpened>]
public record UsageLedger(
    Guid Id,
    string Name,

    [AddFrom<UsageRecorded>(nameof(UsageRecorded.Consumed))]
    decimal TotalConsumed,

    [ChildrenFrom<UsageRecorded>(key: nameof(UsageRecorded.Period), identifiedBy: nameof(UsagePeriod.Period))]
    IEnumerable<UsagePeriod> Periods);
