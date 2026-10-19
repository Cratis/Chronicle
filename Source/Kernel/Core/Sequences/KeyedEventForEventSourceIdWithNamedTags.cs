// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents an event with named tags for an event source together with its optional idempotency key.
/// </summary>
/// <param name="Event">The event.</param>
/// <param name="IdempotencyKey">The key identifying the append intent; null or empty for an unkeyed event.</param>
public record KeyedEventForEventSourceIdWithNamedTags(EventForEventSourceIdWithNamedTags Event, string? IdempotencyKey = default);
