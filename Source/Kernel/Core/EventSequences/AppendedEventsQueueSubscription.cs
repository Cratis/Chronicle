// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents a subscription to an appended events queue.
/// </summary>
/// <param name="ObserverKey"><see cref="ObserverKey"/> to observer which is subscribing.</param>
/// <param name="Queue">Queue to subscribe to.</param>
public record AppendedEventsQueueSubscription(ObserverKey ObserverKey, int Queue);
