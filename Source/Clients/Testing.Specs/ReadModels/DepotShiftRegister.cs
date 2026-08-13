// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Model-bound counterpart of <see cref="DepotShiftLog"/>: the same event type appears both as the root
/// <see cref="FromEventAttribute{T}"/> source and as the <see cref="ChildrenFromAttribute{T}"/> source, so the
/// root owns the key resolver and the child must resolve its own.
/// </summary>
/// <param name="Id">Depot register identifier.</param>
/// <param name="Depot">The depot name, auto-mapped from the same event that feeds <see cref="Shifts"/>.</param>
/// <param name="Shifts">Shift entries keyed by <see cref="ShiftLogged.Worker"/>.</param>
[Passive]
[FromEvent<ShiftLogged>]
public record DepotShiftRegister(
    Guid Id,
    string Depot,

    [ChildrenFrom<ShiftLogged>(key: nameof(ShiftLogged.Worker))]
    IEnumerable<ShiftEntry> Shifts);
