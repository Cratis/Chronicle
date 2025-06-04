// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents an implementation of <see cref="IConfigurationForObserverProvider"/>.
/// </summary>
/// <param name="optionsMonitor">The <see cref="IOptionsMonitor{TOptions}"/>for <see cref="ChronicleOptions"/>.</param>
[Singleton]
public class ConfigurationForObserverProvider(IOptionsMonitor<ChronicleOptions> optionsMonitor) : IConfigurationForObserverProvider
{
    /// <inheritdoc/>
    public Task<Observers> GetFor(string observerKey)
    {
        // TODO: Merge with persisted config and attributes.
        return Task.FromResult(optionsMonitor.CurrentValue.Observers);
    }
}
