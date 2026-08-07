// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Integration.for_ReadModels.when_a_composed_read_model_lifts_pii;

/// <summary>
/// The person a retention decision is about. Deliberately not an <see cref="EventSourceId{T}"/> and
/// deliberately not named <c>Id</c>, because that is what the composed rows below need in order to reproduce
/// a row that resolves no compliance subject of its own.
/// </summary>
/// <param name="Value">Actual value.</param>
public record SubjectIdentifier(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Convert from a <see cref="string"/> to a <see cref="SubjectIdentifier"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to convert from.</param>
    public static implicit operator SubjectIdentifier(string value) => new(value);
}

/// <summary>
/// The free-text reason an advisor gave for postponing a deletion — personal data, stored encrypted under
/// the person's own subject.
/// </summary>
/// <param name="Value">Actual value.</param>
[PII]
public record PostponementComment(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Convert from a <see cref="string"/> to a <see cref="PostponementComment"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to convert from.</param>
    public static implicit operator PostponementComment(string value) => new(value);
}

/// <summary>
/// The text of a review note — personal data nested one level down inside a value object.
/// </summary>
/// <param name="Value">Actual value.</param>
[PII]
public record NoteText(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Convert from a <see cref="string"/> to a <see cref="NoteText"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> to convert from.</param>
    public static implicit operator NoteText(string value) => new(value);
}

/// <summary>
/// A value object holding one personal value and one that is not, so a composed row can lift the pair and
/// the release walk has to reach inside it.
/// </summary>
/// <param name="Text">The personal note text.</param>
/// <param name="Author">Who recorded it — not personal data.</param>
public record ReviewNote(NoteText Text, string Author);

/// <summary>
/// A deletion was postponed for the person the event source identifies.
/// </summary>
/// <param name="Comment">The reason the advisor gave.</param>
/// <param name="Note">The review note recorded alongside it.</param>
[EventType]
public record RetentionPostponed(PostponementComment Comment, ReviewNote Note);

/// <summary>
/// The stored, person-scoped read model. Its own key is the person, so Chronicle encrypts both personal
/// values under that person's key at rest and releases them on every read through the kernel.
/// </summary>
/// <param name="Id">The person the row is about.</param>
/// <param name="Comment">The reason the advisor gave.</param>
/// <param name="Note">The review note recorded alongside it.</param>
[FromEvent<RetentionPostponed>]
public record RetentionSubject(string Id, PostponementComment Comment, ReviewNote Note);

#pragma warning restore SA1402
