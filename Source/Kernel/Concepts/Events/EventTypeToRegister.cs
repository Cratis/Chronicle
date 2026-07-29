// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events;

/// <summary>
/// Represents an <see cref="EventTypeDefinition"/> to register together with the <see cref="EventTypeSource"/> it came from.
/// </summary>
/// <param name="Definition">The <see cref="EventTypeDefinition"/> to register.</param>
/// <param name="Source">The <see cref="EventTypeSource"/> the event type came from.</param>
/// <remarks>
/// <see cref="EventTypeDefinition"/> carries the owner and tombstone metadata of an event type but not the source it
/// came from - pairing the two is what makes it possible to register a whole batch of event types in one operation.
/// </remarks>
public record EventTypeToRegister(EventTypeDefinition Definition, EventTypeSource Source);
