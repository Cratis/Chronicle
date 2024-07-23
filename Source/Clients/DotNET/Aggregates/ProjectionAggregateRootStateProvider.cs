// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootStateProvider"/> using a projection.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionAggregateRootStateProvider"/> class.
/// </remarks>
/// <param name="aggregateRoot">The <see cref="AggregateRoot"/> the state is for.</param>
/// <param name="projections"><see cref="IProjections"/> to use for getting state.</param>
public class ProjectionAggregateRootStateProvider(
    AggregateRoot aggregateRoot,
    IProjections projections) : IAggregateRootStateProvider
{
    /// <inheritdoc/>
    public async Task<object?> Provide()
    {
        var result = await projections.GetInstanceByIdForSession(
            aggregateRoot.CorrelationId,
            aggregateRoot.StateType,
            aggregateRoot._eventSourceId);
        return result.Model;
    }

    /// <inheritdoc/>
    public async Task<object?> Update(object? initialState, IEnumerable<object> events)
    {
        var result = await projections.GetInstanceByIdForSessionWithEventsApplied(
            aggregateRoot.CorrelationId,
            aggregateRoot.StateType,
            aggregateRoot._eventSourceId,
            events);
        return result.Model;
    }

    /// <inheritdoc/>
    public Task Dehydrate() =>
        projections.DehydrateSession(
            aggregateRoot.CorrelationId,
            aggregateRoot.StateType,
            aggregateRoot._eventSourceId);
}
