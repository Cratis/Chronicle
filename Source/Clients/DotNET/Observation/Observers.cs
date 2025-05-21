// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObservers"/>.
/// </summary>
/// <param name="eventStore">The <see cref="IEventStore"/>.</param>
public class Observers(IEventStore eventStore) : IObservers
{
    /// <inheritdoc/>
    public IObserverInformationProvider GetInformationProviderFor(ObserverId observerId, EventSequenceId eventSequenceId) =>
        new ObserverInformationProvider(eventStore, observerId, eventSequenceId);
}
