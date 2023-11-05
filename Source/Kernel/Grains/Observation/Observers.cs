// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Persistence.Observation;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObservers"/>.
/// </summary>
public class Observers : Grain, IObservers
{
    readonly IExecutionContextManager _executionContextManager;
    readonly IGrainFactory _grainFactory;
    readonly ProviderFor<IObserverStorage> _observerStorageProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observers"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains outside of Orleans task context.</param>
    /// <param name="observerStorageProvider">Provider for <see cref="IObserverStorage"/>.</param>
    public Observers(
        IExecutionContextManager executionContextManager,
        IGrainFactory grainFactory,
        ProviderFor<IObserverStorage> observerStorageProvider)
    {
        _executionContextManager = executionContextManager;
        _grainFactory = grainFactory;
        _observerStorageProvider = observerStorageProvider;
    }

    /// <inheritdoc/>
    public async Task Rehydrate()
    {
        _ = this.GetPrimaryKeyLong(out var keyAsString);
        var key = ObserversKey.Parse(keyAsString!);

        _executionContextManager.Establish(key.TenantId, _executionContextManager.Current.CorrelationId, key.MicroserviceId);
        var observers = await _observerStorageProvider().GetAllObservers();

        foreach (var observerInfo in observers)
        {
            var observerKey = new ObserverKey(key.MicroserviceId, key.TenantId, observerInfo.EventSequenceId);
            var observer = _grainFactory.GetGrain<IObserver>(observerInfo.ObserverId, keyExtension: observerKey);
            await observer.Ensure();
        }
    }
}
