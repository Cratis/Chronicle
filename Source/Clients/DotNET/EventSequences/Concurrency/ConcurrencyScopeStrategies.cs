// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Aggregates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Represents an implementation of <see cref="IConcurrencyScopeStrategies"/> that provides concurrency strategies based on configuration options.
/// </summary>
/// <param name="options">The <see cref="IOptions{ConcurrencyOptions}"/> containing the concurrency configuration.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> to resolve the concurrency strategy.</param>
public class ConcurrencyScopeStrategies(IOptions<ConcurrencyOptions> options, IServiceProvider serviceProvider) : IConcurrencyScopeStrategies
{
    /// <inheritdoc/>
    public IConcurrencyScopeStrategy GetFor(IEventSequence eventSequence) =>
        (ActivatorUtilities.CreateInstance(serviceProvider, options.Value.DefaultStrategy, eventSequence) as IConcurrencyScopeStrategy)!;

    /// <inheritdoc/>
    public IConcurrencyScopeStrategy GetFor(IAggregateRootContext aggregateRootContext) =>
        (ActivatorUtilities.CreateInstance(serviceProvider, options.Value.AggregateRootStrategy, aggregateRootContext) as IConcurrencyScopeStrategy)!;
}
