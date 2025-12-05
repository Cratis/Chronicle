// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.MongoDB.Projections;

/// <summary>
/// Represents the type of an event.
/// </summary>
/// <param name="Id">The <see cref="EventTypeId"/>.</param>
/// <param name="Generation">The generation of the event type.</param>
public record EventType(EventTypeId Id, EventTypeGeneration Generation);
