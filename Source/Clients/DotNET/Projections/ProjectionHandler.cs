// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjectionHandler"/>.
/// </summary>
/// <param name="eventStore">The event store to use.</param>
/// <param name="projectionId">The identifier of the projection.</param>
/// <param name="readModelType">The type of the read model.</param>
/// <param name="containerName">The container name of the read model (collection, table, etc.).</param>
/// <param name="eventSequenceId">The event sequence identifier.</param>
public class ProjectionHandler(
    IEventStore eventStore,
    ProjectionId projectionId,
    Type readModelType,
    ReadModelContainerName containerName,
    EventSequenceId eventSequenceId) : IProjectionHandler
{
    /// <inheritdoc/>
    public ProjectionId Id => projectionId;

    /// <inheritdoc/>
    public Type ReadModelType => readModelType;

    /// <inheritdoc/>
    public ReadModelContainerName ContainerName => containerName;

    /// <inheritdoc/>
    public Task<IEnumerable<FailedPartition>> GetFailedPartitions() =>
         eventStore.FailedPartitions.GetFailedPartitionsFor(Id.Value);

    /// <inheritdoc/>
    public async Task<ProjectionState> GetState()
    {
        var servicesAccessor = (eventStore.Connection as IChronicleServicesAccessor)!;
        var observers = await servicesAccessor.Services.Observers.AllObservers(new Contracts.Observation.AllObserversRequest
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace
        });
        var state = observers.FirstOrDefault(o => o.Id == projectionId.ToString() && o.EventSequenceId == eventSequenceId.ToString());
        if (state is null)
        {
            return new ProjectionState(ObserverRunningState.Unknown, false, 0, 0);
        }

        return new ProjectionState(
            state.RunningState.ToClient(),
            state.IsSubscribed,
            state.NextEventSequenceNumber,
            state.LastHandledEventSequenceNumber);
    }
}
