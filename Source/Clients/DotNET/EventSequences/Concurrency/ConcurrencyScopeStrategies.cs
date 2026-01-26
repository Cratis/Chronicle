// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.EventSequences.Concurrency;

/// <summary>
/// Represents an implementation of <see cref="IConcurrencyScopeStrategies"/> that provides concurrency strategies based on configuration options.
/// </summary>
/// <param name="options">The <see cref="ConcurrencyOptions"/> containing the concurrency configuration.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/> to resolve the concurrency strategy.</param>
public class ConcurrencyScopeStrategies(ConcurrencyOptions options, IServiceProvider serviceProvider) : IConcurrencyScopeStrategies
{
    /// <inheritdoc/>
    public IConcurrencyScopeStrategy GetFor(IEventSequence eventSequence) =>
        (ActivatorUtilities.CreateInstance(serviceProvider, options.DefaultStrategy, eventSequence) as IConcurrencyScopeStrategy)!;
}
