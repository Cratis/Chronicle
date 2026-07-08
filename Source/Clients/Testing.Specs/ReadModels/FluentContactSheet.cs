// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model projected by a standalone fluent <see cref="Projections.IProjectionFor{TReadModel}"/> class
/// (<see cref="FluentContactSheetProjection"/>) rather than model-bound attributes. Used to verify that
/// <see cref="ReadModelScenario{TReadModel}"/> can build and drive a fluent projection class — the path that
/// previously threw an arity mismatch because its definition builder was nested in the generic scenario type.
/// </summary>
/// <param name="Id">Sheet identifier.</param>
/// <param name="Year">The year the sheet covers.</param>
/// <param name="Contacts">Contact entries keyed by <see cref="ContactAssigned.ContactId"/> (a concept).</param>
public record FluentContactSheet(
    SheetId Id,
    int Year,
    IEnumerable<ContactEntry> Contacts);
