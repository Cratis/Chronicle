// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Aggregates;

/// <summary>
/// Represents a null implementation of <see cref="IAggregateRootStateProvider"/>.
/// </summary>
public class NullAggregateRootStateProvider : IAggregateRootStateProvider
{
    /// <summary>
    /// Gets the singleton instance of <see cref="NullAggregateRootStateProvider"/>.
    /// </summary>
    public static readonly NullAggregateRootStateProvider Instance = new();

    /// <inheritdoc/>
    public Task<object?> Provide() => Task.FromResult<object?>(null);

    /// <inheritdoc/>
    public Task<object?> Update(object? initialState, IEnumerable<object> events) => Task.FromResult<object?>(null);
}
