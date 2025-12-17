// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.Events;

/// <summary>
/// Represents an implementation of <see cref="IEventLog"/> for testing.
/// </summary>
/// <param name="eventTypes"><see cref="IEventTypes"/> for mapping types.</param>///
/// <param name="events">Optional events to populate the event log with.</param>
public class EventLogForTesting(IEventTypes eventTypes, params EventForEventSourceId[] events) : EventSequenceForTesting(eventTypes, events), IEventLog;
