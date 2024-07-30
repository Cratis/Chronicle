// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Aggregates;

namespace Cratis.Chronicle.Orleans.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootStateProviders"/> for testing.
/// </summary>
public class AggregateRootStateProvidersForTesting : IAggregateRootStateProviders
{
    /// <inheritdoc/>
    public Task<IAggregateRootStateProvider<TState>> CreateFor<TState>(IAggregateRootContext aggregateRootContext) =>
        Task.FromResult<IAggregateRootStateProvider<TState>>(new AggregateRootStateProviderForTesting<TState>());
}
