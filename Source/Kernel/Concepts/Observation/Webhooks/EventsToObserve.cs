// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation.Webhooks;

/// <summary>
/// Represents the events to observer.
/// </summary>
/// <param name="Partition">The partition key of the events.</param>
/// <param name="Events">The events to observe.</param>
public record EventsToObserve(string Partition, IReadOnlyList<AppendedEvent> Events);