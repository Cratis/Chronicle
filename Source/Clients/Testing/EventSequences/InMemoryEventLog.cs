// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents an in-memory implementation of <see cref="IEventLog"/> for testing purposes.
/// </summary>
/// <param name="eventTypes">The <see cref="IEventTypes"/> for mapping CLR types to <see cref="EventType"/>.</param>
public class InMemoryEventLog(IEventTypes eventTypes) : InMemoryEventSequence(EventSequenceId.Log, eventTypes), IEventLog;
