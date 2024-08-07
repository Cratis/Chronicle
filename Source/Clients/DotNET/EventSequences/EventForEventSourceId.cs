// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents an event and the <see cref="EventSourceId"/> it is for.
/// </summary>
/// <param name="EventSourceId"><see cref="EventSourceId"/> the event is for.</param>
/// <param name="Event">The actual event.</param>
/// <param name="causation">The causation for the event.</param>
public record EventForEventSourceId(EventSourceId EventSourceId, object Event, Causation causation);
