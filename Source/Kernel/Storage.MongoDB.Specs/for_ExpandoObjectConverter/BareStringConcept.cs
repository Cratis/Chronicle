// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.for_ExpandoObjectConverter;

/// <summary>
/// A bare string concept used as the element type of a read-model collection, to verify the MongoDB
/// converter persists a list of bare concepts rather than dropping it.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record BareStringConcept(string Value) : ConceptAs<string>(Value);
