// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model projected by <see cref="DepotShiftBoardProjection"/>, the <c>AddChild(...)</c> spelling of the
/// same shape <see cref="DepotShiftLog"/> covers with <c>Children(...).From&lt;T&gt;</c>.
/// </summary>
/// <param name="Id">Depot board identifier.</param>
/// <param name="Depot">The depot name, mapped from the same event that feeds <see cref="Shifts"/>.</param>
/// <param name="Shifts">Shift entries keyed by <see cref="ShiftLogged.Worker"/>.</param>
public record DepotShiftBoard(
    Guid Id,
    string Depot,
    IEnumerable<ShiftEntry> Shifts);
