// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Child-only counterpart of <see cref="DepotShiftRegister"/>: no root creation event, so the root only comes into
/// existence through the <see cref="ShiftLogged"/> child events.
/// </summary>
/// <param name="Id">Depot roster identifier.</param>
/// <param name="Shifts">Shift entries keyed by <see cref="ShiftLogged.Worker"/>.</param>
[Passive]
public record ChildOnlyDepotRoster(
    Guid Id,

    [ChildrenFrom<ShiftLogged>(key: nameof(ShiftLogged.Worker))]
    IEnumerable<ShiftEntry> Shifts);
