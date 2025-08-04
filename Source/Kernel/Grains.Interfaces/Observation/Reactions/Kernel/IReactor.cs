// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Grains.Observation.Reactors.Kernel;

/// <summary>
/// Defines a kernel reactor that will process events.
/// </summary>
public interface IReactor
{
    /// <summary>
    /// Processes the next batch of events.
    /// </summary>
    /// <param name="events">The events to process.</param>
    /// <returns>The result of the processing.</returns>
    Task<ObserverSubscriberResult> OnNext(IEnumerable<AppendedEvent> events);
}
