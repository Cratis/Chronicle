// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Projections;

namespace Cratis.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootStateProvider"/> using a projection.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionAggregateRootStateProvider"/> class.
/// </remarks>
/// <param name="aggregateRoot">The <see cref="AggregateRoot"/> the state is for.</param>
/// <param name="immediateProjections"><see cref="IImmediateProjections"/> to use for getting state.</param>
public class ProjectionAggregateRootStateProvider(
    AggregateRoot aggregateRoot,
    IImmediateProjections immediateProjections) : IAggregateRootStateProvider
{
    /// <inheritdoc/>
    public async Task<object?> Provide()
    {
        var result = await immediateProjections.GetInstanceByIdForSession(
            aggregateRoot.CorrelationId,
            aggregateRoot.StateType,
            aggregateRoot._eventSourceId);
        return result.Model;
    }

    /// <inheritdoc/>
    public async Task<object?> Update(object? initialState, IEnumerable<object> events)
    {
        var result = await immediateProjections.GetInstanceByIdForSessionWithEventsApplied(
            aggregateRoot.CorrelationId,
            aggregateRoot.StateType,
            aggregateRoot._eventSourceId,
            events);
        return result.Model;
    }

    /// <inheritdoc/>
    public Task Dehydrate() =>
        immediateProjections.DehydrateSession(
            aggregateRoot.CorrelationId,
            aggregateRoot.StateType,
            aggregateRoot._eventSourceId);
}
