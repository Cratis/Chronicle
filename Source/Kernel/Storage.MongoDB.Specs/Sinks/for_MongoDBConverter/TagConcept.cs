// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_MongoDBConverter;

/// <summary>
/// A bare string concept used as the element type of a read-model collection.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record TagConcept(string Value) : ConceptAs<string>(Value);
