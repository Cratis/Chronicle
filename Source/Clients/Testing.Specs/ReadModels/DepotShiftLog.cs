// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model projected by <see cref="DepotShiftLogProjection"/>, where one and the same event
/// (<see cref="ShiftLogged"/>) maps a parent scalar and feeds the child collection.
/// </summary>
/// <param name="Id">Depot log identifier.</param>
/// <param name="Depot">The depot name, mapped from the same event that feeds <see cref="Shifts"/>.</param>
/// <param name="Shifts">Shift entries keyed by <see cref="ShiftLogged.Worker"/>.</param>
public record DepotShiftLog(
    Guid Id,
    string Depot,
    IEnumerable<ShiftEntry> Shifts);
