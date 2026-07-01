// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model carrying two child collections on the same event source: one keyed by a
/// <see cref="ContactId"/> concept over a <see cref="Guid"/>, and one keyed by a <see cref="DateOnly"/>.
/// Used to verify that the presence of a working concept-keyed collection does not stop the
/// <see cref="DateOnly"/>-keyed collection from materializing (and vice versa).
/// </summary>
/// <param name="Id">Sheet identifier.</param>
/// <param name="Year">The year the sheet covers.</param>
/// <param name="Contacts">Contact entries keyed by <see cref="ContactAssigned.ContactId"/> (a concept).</param>
/// <param name="Days">Day entries keyed by <see cref="ConceptDayWorked.Day"/> (a <see cref="DateOnly"/>).</param>
[Passive]
[FromEvent<ConceptSheetStarted>]
public record MixedKeySheet(
    SheetId Id,
    int Year,

    [ChildrenFrom<ContactAssigned>(key: nameof(ContactAssigned.ContactId))]
    IEnumerable<ContactEntry> Contacts,

    [ChildrenFrom<ConceptDayWorked>(key: nameof(ConceptDayWorked.Day))]
    IEnumerable<ConceptDayEntry> Days);
