// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;

namespace Cratis.EventSequences;

/// <summary>
/// Exception that gets thrown when an event type is not marked with the <see cref="EventTypeAttribute"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="UnknownEventType"/> class.
/// </remarks>
/// <param name="type">Type missing the <see cref="EventTypeAttribute"/>.</param>
public class UnknownEventType(Type type) : Exception($"Type '{type.FullName}' is unknown. Have you remembered to mark it with the `[EventType]` attribute?")
{
}
