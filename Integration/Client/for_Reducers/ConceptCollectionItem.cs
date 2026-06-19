// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_Reducers;

/// <summary>
/// A bare string concept used as the element type of a read-model collection.
/// </summary>
/// <param name="Value">The underlying value.</param>
public record ConceptCollectionItem(string Value) : ConceptAs<string>(Value);
