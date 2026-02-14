// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Seeding;

/// <summary>
/// Represents a group of seed entries for a specific event source.
/// </summary>
/// <param name="EventSourceId">The event source identifier.</param>
/// <param name="Entries">The seed entries for this event source.</param>
public record EventSourceSeedGroup(
    string EventSourceId,
    IEnumerable<SeedEntry> Entries);
