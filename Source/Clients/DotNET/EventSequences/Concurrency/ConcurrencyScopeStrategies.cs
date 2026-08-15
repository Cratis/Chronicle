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
        (CreateStrategy(eventSequence) as IConcurrencyScopeStrategy)!;

    /// <summary>
    /// Check whether a strategy type asks to be given the <see cref="ConcurrencyOptions"/>.
    /// </summary>
    /// <param name="strategy">The strategy <see cref="Type"/> to check.</param>
    /// <returns>True if a constructor declares a <see cref="ConcurrencyOptions"/> parameter, false if not.</returns>
    static bool WantsConcurrencyOptions(Type strategy) =>
        strategy.GetConstructors().Any(constructor =>
            constructor.GetParameters().Any(parameter => parameter.ParameterType == typeof(ConcurrencyOptions)));

    /// <summary>
    /// Create the configured strategy, handing it the <see cref="ConcurrencyOptions"/> if it asks for them.
    /// </summary>
    /// <param name="eventSequence">The <see cref="IEventSequence"/> the strategy resolves scopes for.</param>
    /// <returns>The created strategy.</returns>
    /// <remarks>
    /// The options are not in the container - they are handed to this type directly - so a strategy that wants them
    /// has to be given them as a constructor argument. Only a strategy that declares a
    /// <see cref="ConcurrencyOptions"/> parameter gets one, because an argument no constructor accepts makes
    /// <see cref="ActivatorUtilities"/> reject that constructor outright. A custom strategy taking only the event
    /// sequence is therefore created exactly as it was before the options existed.
    /// </remarks>
    object CreateStrategy(IEventSequence eventSequence) =>
        WantsConcurrencyOptions(options.DefaultStrategy)
            ? ActivatorUtilities.CreateInstance(serviceProvider, options.DefaultStrategy, eventSequence, options)
            : ActivatorUtilities.CreateInstance(serviceProvider, options.DefaultStrategy, eventSequence);
}
