// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines a system that knows about observers.
/// </summary>
public interface IObservers
{
    /// <summary>
    /// Gets the <see cref="IObserverInformationProvider"/> for an observer.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/>.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> that the observer observes events from.</param>
    /// <returns>The <see cref="IObserverInformationProvider"/>.</returns>
    IObserverInformationProvider GetInformationProviderFor(ObserverId observerId, EventSequenceId eventSequenceId);
}