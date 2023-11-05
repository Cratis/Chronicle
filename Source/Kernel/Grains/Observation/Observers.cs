// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.Configuration;
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
    readonly ProviderFor<IObserverStorage> _observerStorageProvider;
    readonly KernelConfiguration _configuration;
    readonly Microservices _microservices;

    /// <summary>
    /// Initializes a new instance of the <see cref="Observers"/> class.
    /// </summary>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="observerStorageProvider">Provider for <see cref="IObserverStorage"/>.</param>
    /// <param name="configuration">The Kernel configuration.</param>
    /// <param name="microservices">All configured microservices.</param>
    public Observers(
        IExecutionContextManager executionContextManager,
        ProviderFor<IObserverStorage> observerStorageProvider,
        KernelConfiguration configuration,
        Microservices microservices)
    {
        _executionContextManager = executionContextManager;
        _observerStorageProvider = observerStorageProvider;
        _configuration = configuration;
        _microservices = microservices;
    }

    /// <inheritdoc/>
    public async Task Rehydrate()
    {
        foreach (var microserviceId in _microservices.GetMicroserviceIds())
        {
            foreach (var tenant in _configuration.Tenants.GetTenantIds())
            {
                _executionContextManager.Establish(tenant, _executionContextManager.Current.CorrelationId, microserviceId);
                var observers = await _observerStorageProvider().GetAllObservers();

                foreach (var observerInfo in observers)
                {
                    var observer = GrainFactory.GetGrain<IObserver>(observerInfo.ObserverId, keyExtension: new ObserverKey(microserviceId, tenant, observerInfo.EventSequenceId));
                    await observer.Ensure();
                }
            }
        }
    }
}
