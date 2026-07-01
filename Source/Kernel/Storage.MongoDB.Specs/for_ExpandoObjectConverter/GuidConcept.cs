// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter;

/// <summary>
/// A bare <see cref="System.Guid"/> concept, used to verify the converter persists a collection of
/// concepts over a non-string primitive (not just <see cref="string"/>) by its underlying value.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record GuidConcept(Guid Value) : ConceptAs<Guid>(Value);
