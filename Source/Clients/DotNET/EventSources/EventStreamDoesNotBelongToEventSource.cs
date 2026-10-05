// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources;

/// <summary>
/// The exception that is thrown when an event stream is used that the event source does not declare.
/// </summary>
/// <param name="eventSource">The name of the event source.</param>
/// <param name="stream">The name of the stream.</param>
public class EventStreamDoesNotBelongToEventSource(string eventSource, string stream)
    : Exception($"The event stream '{stream}' is not declared by the event source '{eventSource}'.");
