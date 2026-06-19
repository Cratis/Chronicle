// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Integration.for_Reducers;

/// <summary>
/// Read model mirroring the shape that surfaced the bug — a scalar concept (which persists fine)
/// alongside a collection of bare concepts (which previously persisted empty).
/// </summary>
/// <param name="Latest">The most recently added item — a scalar concept.</param>
/// <param name="Items">The collection of <see cref="ConceptCollectionItem"/>.</param>
public record ConceptCollectionReadModel(ConceptCollectionItem Latest, IReadOnlyList<ConceptCollectionItem> Items);
