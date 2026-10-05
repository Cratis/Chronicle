// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSources;

/// <summary>
/// The exception that is thrown when an event source is referenced that has not been discovered.
/// </summary>
/// <param name="reference">The type or name that was referenced.</param>
public class UnknownEventSource(string reference)
    : Exception($"The event source '{reference}' is not a discovered event source. Declare it as a type implementing IEventSource with an [EventSource] attribute.");
