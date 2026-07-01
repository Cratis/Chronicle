// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter;

/// <summary>
/// A read model with top-level lists of bare concepts — over both a <see cref="string"/> and a
/// <see cref="Guid"/> — used to verify the MongoDB converter round-trips a concept collection by its
/// underlying value instead of persisting it empty, for any concept underlying type.
/// </summary>
/// <param name="Id">Identifier.</param>
/// <param name="Tags">The list of bare string concepts.</param>
/// <param name="Refs">The list of bare <see cref="Guid"/> concepts.</param>
public record ConceptListTargetType(Guid Id, IEnumerable<BareStringConcept> Tags, IEnumerable<GuidConcept> Refs);
