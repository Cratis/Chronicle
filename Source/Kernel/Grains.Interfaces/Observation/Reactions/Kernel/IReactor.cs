// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Grains.EventSequences;

namespace Cratis.Chronicle.Grains.Observation.Reactors.Kernel;

/// <summary>
/// Defines a kernel reactor that will process events.
/// </summary>
public interface IReactor
{
    /// <summary>
    /// Initializes the reactor with the necessary event types and converters.
    /// </summary>
    /// <param name="eventSerializer">The <see cref="IEventSerializer"/> to use for event serialization and deserialization.</param>
    void Initialize(IEventSerializer eventSerializer);

    /// <summary>
    /// Processes the next batch of events.
    /// </summary>
    /// <param name="events">The events to process.</param>
    /// <returns>The result of the processing.</returns>
    Task<ObserverSubscriberResult> OnNext(IEnumerable<AppendedEvent> events);
}
