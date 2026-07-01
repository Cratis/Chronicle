// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Projections.Concepts;

namespace Cratis.Chronicle.Integration.Projections.Events;

/// <summary>
/// Emitted when a whole collection of bare concepts is assigned in one event — the bulk-list shape a
/// projection materializes into a concept collection.
/// </summary>
/// <param name="Tags">The collection of <see cref="StringConcept"/>.</param>
[EventType]
public record ConceptTagsAssigned(IReadOnlyList<StringConcept> Tags);
