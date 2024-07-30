// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Aggregates;

namespace Cratis.Chronicle.Orleans.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootStateProvider{TState}"/> for testing.
/// </summary>
/// <typeparam name="TState">Type of state.</typeparam>
public class AggregateRootStateProviderForTesting<TState> : IAggregateRootStateProvider<TState>
{
    /// <inheritdoc/>
    public Task Dehydrate() => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<TState?> Provide() => Task.FromResult<TState?>(default);

    /// <inheritdoc/>
    public Task<TState?> Update(TState? initialState, IEnumerable<object> events) => Task.FromResult<TState?>(default);
}
