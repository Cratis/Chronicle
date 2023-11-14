// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Projections;

namespace Aksio.Cratis.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootStateProvider"/> using a projection.
/// </summary>
public class ProjectionAggregateRootStateProvider : IAggregateRootStateProvider
{
    readonly AggregateRoot _aggregateRoot;
    readonly IImmediateProjections _immediateProjections;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionAggregateRootStateProvider"/> class.
    /// </summary>
    /// <param name="aggregateRoot">The <see cref="AggregateRoot"/> the state is for.</param>
    /// <param name="immediateProjections"><see cref="IImmediateProjections"/> to use for getting state.</param>
    public ProjectionAggregateRootStateProvider(
        AggregateRoot aggregateRoot,
        IImmediateProjections immediateProjections)
    {
        _aggregateRoot = aggregateRoot;
        _immediateProjections = immediateProjections;
    }

    /// <inheritdoc/>
    public async Task<object?> Provide()
    {
        var result = await _immediateProjections.GetInstanceById(_aggregateRoot.StateType, _aggregateRoot._eventSourceId);
        return result.Model;
    }

    /// <inheritdoc/>
    public Task<object?> Update(IEnumerable<object> events) => Task.FromResult(_aggregateRoot.GetState());
}
