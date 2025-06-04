// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionHandler"/>.
/// </summary>
/// <param name="eventStore">The event store to use.</param>
/// <param name="projectionId">The identifier of the projection.</param>
/// <param name="eventSequenceId">The event sequence identifier.</param>
public class ProjectionHandler(
    IEventStore eventStore,
    ProjectionId projectionId,
    EventSequenceId eventSequenceId) : IProjectionHandler
{
    /// <inheritdoc/>
    public ProjectionId Id => projectionId;

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> GetFailedPartitions() =>
         eventStore.FailedPartitions.GetFailedPartitionsFor(Id.Value);

    /// <inheritdoc/>
    public async Task<ProjectionState> GetState()
    {
        var request = new Contracts.Observation.GetObserverInformationRequest
        {
            ObserverId = projectionId,
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            EventSequenceId = eventSequenceId
        };
        var servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;
        var state = await servicesAccessor.Services.Observers.GetObserverInformation(request);
        return new ProjectionState(
            state.RunningState.ToClient(),
            state.IsSubscribed,
            state.NextEventSequenceNumber,
            state.LastHandledEventSequenceNumber);
    }
}
