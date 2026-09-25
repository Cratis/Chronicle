// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObservers"/>.
/// </summary>
/// <param name="eventStore"><see cref="IEventStore"/> the observers belong to.</param>
public class Observers(IEventStore eventStore) : IObservers
{
    readonly IChronicleServicesAccessor _servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;

    /// <inheritdoc/>
    public async Task<IEnumerable<ObserverInformation>> GetAll()
    {
        var observers = await _servicesAccessor.Services.Observers.GetObservers(new()
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace
        });

        return observers.Select(observer => observer.ToClient()).ToArray();
    }

    /// <inheritdoc/>
    public async Task<ObserverRemovalResult> Remove(ObserverId observerId)
    {
        var response = await _servicesAccessor.Services.Observers.RemoveObserver(new()
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            ObserverId = observerId,
            EventSequenceId = EventSequenceId.Log
        });

        return new ObserverRemovalResult((ObserverRemovalOutcome)(int)response.Outcome, response.BlockingNamespace);
    }
}
