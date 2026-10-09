// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Sinks;

/// <summary>
/// The exception that is thrown when an event target schema carries compliance metadata, which public event targets do not yet support.
/// </summary>
/// <param name="eventType">The target <see cref="EventType"/>.</param>
public class EventSequenceTargetCarriesCompliance(EventType eventType)
    : Exception($"The event target '{eventType}' carries compliance metadata. Public event targets do not support it yet; it is refused rather than published unprotected or protected twice.");
