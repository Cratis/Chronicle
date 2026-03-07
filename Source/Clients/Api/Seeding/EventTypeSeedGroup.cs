// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Api.Seeding;

/// <summary>
/// Represents a group of seed entries for a specific event type.
/// </summary>
/// <param name="EventTypeId">The event type identifier.</param>
/// <param name="Entries">The seed entries for this event type.</param>
public record EventTypeSeedGroup(
    string EventTypeId,
    IEnumerable<SeedEntry> Entries);
