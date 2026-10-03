// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources;

/// <summary>
/// The exception that is thrown when explicit routing values contradict the event source an event is appended through.
/// </summary>
/// <param name="eventSource">The name of the event source.</param>
/// <param name="dimension">The routing dimension that contradicts it.</param>
/// <param name="expected">The value the definition implies.</param>
/// <param name="actual">The explicit value.</param>
public class EventRoutingContradictsEventSource(string eventSource, string dimension, string expected, string actual)
    : Exception($"The explicit {dimension} '{actual}' contradicts the event source '{eventSource}', which implies '{expected}'.");
