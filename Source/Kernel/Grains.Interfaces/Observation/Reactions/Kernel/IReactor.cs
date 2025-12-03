// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grains.EventTypes.Kernel;
using Cratis.Chronicle.Json;

namespace Cratis.Chronicle.Grains.Observation.Reactors.Kernel;

/// <summary>
/// Defines a kernel reactor that will process events.
/// </summary>
public interface IReactor
{
    /// <summary>
    /// Initializes the reactor with the necessary event types and converters.
    /// </summary>
    /// <param name="eventTypes">The <see cref="IEventTypes"/> for working with event types.</param>
    /// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for converting between expando objects to and from json.</param>
    /// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for JSON serialization.</param>
    void Initialize(IEventTypes eventTypes, IExpandoObjectConverter expandoObjectConverter, JsonSerializerOptions jsonSerializerOptions);

    /// <summary>
    /// Processes the next batch of events.
    /// </summary>
    /// <param name="events">The events to process.</param>
    /// <returns>The result of the processing.</returns>
    Task<ObserverSubscriberResult> OnNext(IEnumerable<AppendedEvent> events);
}
