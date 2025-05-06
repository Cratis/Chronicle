// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionHandler"/>.
/// </summary>
/// <param name="eventStore">The event store to use.</param>
/// <param name="definition">The projection definition.</param>
public class ProjectionHandler(
    IEventStore eventStore,
    ProjectionDefinition definition) : IProjectionHandler
{
    /// <inheritdoc/>
    public ProjectionDefinition Definition => definition;

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> GetFailedPartitions() =>
         eventStore.FailedPartitions.GetFailedPartitionsFor(Definition.Identifier);

    /// <inheritdoc/>
    public async Task<ProjectionState> GetState()
    {
        var request = new Contracts.Observation.GetObserverInformationRequest
        {
            ObserverId = definition.Identifier,
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            EventSequenceId = definition.EventSequenceId
        };
        var state = await eventStore.Connection.Services.Observers.GetObserverInformation(request);
        return new ProjectionState(
            state.RunningState.ToClient(),
            state.IsSubscribed,
            state.NextEventSequenceNumber,
            state.LastHandledEventSequenceNumber);
    }
}
