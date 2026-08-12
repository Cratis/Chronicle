// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The exception that is thrown when a concurrency scope narrows by an event source different from its dictionary label.
/// </summary>
/// <param name="scopeLabel">The dictionary label for the scope.</param>
/// <param name="scopedEventSourceId">The event source the scope narrows by.</param>
public class ConcurrencyScopeEventSourceIdDoesNotMatchLabel(EventSourceId scopeLabel, EventSourceId scopedEventSourceId)
    : Exception($"Concurrency scope label '{scopeLabel}' must match its narrowed event source '{scopedEventSourceId}'.");
