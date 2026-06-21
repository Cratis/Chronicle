// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model with a top-level collection of bare concepts, used to verify the in-memory sink
/// round-trips collection-of-concept elements as their unwrapped underlying value.
/// </summary>
/// <param name="Id">Identifier.</param>
/// <param name="Items">The collection of <see cref="ConceptListItem"/>.</param>
[Passive]
[FromEvent<ConceptListSet>]
public record ConceptListReadModel(Guid Id, IReadOnlyList<ConceptListItem> Items);
