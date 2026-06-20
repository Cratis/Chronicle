// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event carrying a collection of bare concepts.
/// </summary>
/// <param name="Items">The collection of <see cref="ConceptListItem"/>.</param>
[EventType]
public record ConceptListSet(IReadOnlyList<ConceptListItem> Items);
