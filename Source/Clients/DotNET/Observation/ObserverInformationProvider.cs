// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserverInformationProvider"/>.
/// </summary>
/// <param name="eventStore">The <see cref="IEventStore"/>.</param>
/// <param name="observerId">The <see cref="ObserverId"/>.</param>
/// <param name="eventSequenceId">The <see cref="EventSequenceId"/>.</param>
[IgnoreConvention]
public class ObserverInformationProvider(
    IEventStore eventStore,
    ObserverId observerId,
    EventSequenceId eventSequenceId) : IObserverInformationProvider
{
    /// <inheritdoc/>
    public async Task<ObserverState> GeState()
    {
        var request = new Contracts.Observation.GetObserverInformationRequest
        {
            ObserverId = observerId,
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            EventSequenceId = eventSequenceId
        };
        var state = await eventStore.Connection.Services.Observers.GetObserverInformation(request);
        return new(
            state.RunningState.ToClient(),
            state.IsSubscribed,
            state.NextEventSequenceNumber,
            state.LastHandledEventSequenceNumber);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> GetFailedPartitions() =>
        eventStore.FailedPartitions.GetFailedPartitionsFor(observerId);
}
