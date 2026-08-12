// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The exception that is thrown when more than one concurrency scope is supplied for the same event source id label.
/// </summary>
/// <param name="eventSourceId">The duplicate <see cref="EventSourceId"/> label.</param>
public class DuplicateConcurrencyScopeForEventSourceId(EventSourceId eventSourceId)
    : Exception($"A concurrency scope for event source id key '{eventSourceId}' has already been supplied.");
