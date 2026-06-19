// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_Reducers;

/// <summary>
/// Emitted when an item is added to a concept collection.
/// </summary>
/// <param name="Item">The added <see cref="ConceptCollectionItem"/>.</param>
[EventType]
public record ConceptCollectionItemAdded(ConceptCollectionItem Item);
