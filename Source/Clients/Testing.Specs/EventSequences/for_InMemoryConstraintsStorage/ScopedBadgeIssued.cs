// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_InMemoryConstraintsStorage;

/// <summary>
/// Event whose badge number is unique per event stream by <see cref="UniqueBadgeNumberPerStream"/>.
/// </summary>
/// <param name="BadgeNumber">The badge number.</param>
[EventType]
public record ScopedBadgeIssued(string BadgeNumber);
