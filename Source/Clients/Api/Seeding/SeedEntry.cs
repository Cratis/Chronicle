// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Seeding;

/// <summary>
/// Represents a seeded event entry for API usage.
/// </summary>
/// <param name="EventSourceId">The event source identifier.</param>
/// <param name="EventTypeId">The event type identifier.</param>
/// <param name="Content">The JSON content of the event.</param>
public record SeedEntry(
    string EventSourceId,
    string EventTypeId,
    string Content);

