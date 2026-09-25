// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// The event types and admission result for a projection read protected by event-log concurrency.
/// </summary>
/// <param name="IsEventSourceKeyed">Whether every included event resolves to its own event source.</param>
/// <param name="HasJoins">Whether the definition includes joins.</param>
/// <param name="HasChildren">Whether it has child or parent hierarchies.</param>
/// <param name="SubscribesToAllEvents">Whether the projection consumes an open-ended set of event types.</param>
/// <param name="EventTypes">Every projected event type, including removal types.</param>
public record DecisionProjectionShape(bool IsEventSourceKeyed, bool HasJoins, bool HasChildren, bool SubscribesToAllEvents, IReadOnlyList<EventType> EventTypes);
