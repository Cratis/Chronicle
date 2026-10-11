// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Events;

/// <summary>
/// The exception that is thrown when a pinned generation cannot be delivered.
/// </summary>
/// <param name="eventType">The unavailable pinned event type.</param>
public class PinnedEventTypeGenerationUnavailable(EventType eventType) : Exception($"Pinned event type generation '{eventType}' is unavailable.");
