// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Root read model projected from <see cref="SheetStarted"/> with a child collection keyed by a
/// <see cref="DateOnly"/> value, used to verify that the in-memory harness materializes child
/// collections keyed by non-concept value-type primitives.
/// </summary>
/// <param name="Id">Sheet identifier.</param>
/// <param name="Year">The year the sheet covers.</param>
/// <param name="Days">Day entries keyed by <see cref="DayWorked.Day"/>.</param>
[Passive]
[FromEvent<SheetStarted>]
public record Sheet(
    SheetId Id,
    int Year,

    [ChildrenFrom<DayWorked>(key: nameof(DayWorked.Day))]
    IEnumerable<DayEntry> Days);
