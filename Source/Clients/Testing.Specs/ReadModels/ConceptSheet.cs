// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Root read model projected from <see cref="ConceptSheetStarted"/> with a child collection keyed by a
/// <see cref="DateOnly"/> value whose children carry a concept-typed value (<see cref="WorkHours"/>).
/// Used to verify that the in-memory harness materializes child collections keyed by a value-type
/// primitive even when the child body carries a concept value.
/// </summary>
/// <param name="Id">Sheet identifier.</param>
/// <param name="Year">The year the sheet covers.</param>
/// <param name="Days">Day entries keyed by <see cref="ConceptDayWorked.Day"/>.</param>
[Passive]
[FromEvent<ConceptSheetStarted>]
public record ConceptSheet(
    SheetId Id,
    int Year,

    [ChildrenFrom<ConceptDayWorked>(key: nameof(ConceptDayWorked.Day))]
    IEnumerable<ConceptDayEntry> Days);
