// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model with a child collection keyed by a <see cref="DateOnly"/> day, where the children live on
/// their own event source and are linked to the parent via an explicit
/// <see cref="ChildrenFromAttribute{TEvent}.ParentKey"/>. Used to verify the in-memory harness materializes
/// cross-stream <see cref="DateOnly"/>-keyed child collections.
/// </summary>
/// <param name="Id">Ledger identifier.</param>
/// <param name="Name">The ledger name.</param>
/// <param name="Days">Day lines keyed by <see cref="DayRaised.Day"/> (a <see cref="DateOnly"/>).</param>
[Passive]
[FromEvent<LedgerOpened>]
public record DayLedger(
    Guid Id,
    string Name,

    [ChildrenFrom<DayRaised>(
        key: nameof(DayRaised.Day),
        identifiedBy: nameof(DayLine.Day),
        parentKey: nameof(DayRaised.LedgerId))]
    IEnumerable<DayLine> Days);
