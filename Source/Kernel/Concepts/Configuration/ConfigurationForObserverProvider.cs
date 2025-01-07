// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Concepts.Configuration;

/// <summary>
/// Represents an implementation of <see cref="IProvideConfigurationForObserver"/>.
/// </summary>
/// <param name="optionsMonitor">The <see cref="IOptionsMonitor{TOptions}"/>for <see cref="ChronicleOptions"/>.</param>
public class ConfigurationForObserverProvider(IOptionsMonitor<ChronicleOptions> optionsMonitor) : IProvideConfigurationForObserver
{
    /// <inheritdoc/>
    public Task<Observers> GetFor(ObserverSubscriberKey observerSubscriberKey)
    {
        // TODO: Merge with persisted config and attributes.
        return Task.FromResult(optionsMonitor.CurrentValue.Observers);
    }
}
