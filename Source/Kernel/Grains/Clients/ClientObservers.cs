// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Clients;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;
using TenantsConfig = Aksio.Cratis.Kernel.Configuration.Tenants;

namespace Aksio.Cratis.Kernel.Grains.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientObservers"/>.
/// </summary>
public class ClientObservers : Grain, IClientObservers
{
    readonly TenantsConfig _tenants;
    readonly ILogger<ClientObservers> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientObservers"/> class.
    /// </summary>
    /// <param name="tenants">The configured <see cref="TenantsConfig"/>.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ClientObservers(
        TenantsConfig tenants,
        ILogger<ClientObservers> logger)
    {
        _tenants = tenants;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Register(ConnectionId connectionId, IEnumerable<ClientObserverRegistration> registrations)
    {
        _logger.RegisterObservers();

        var microserviceId = (MicroserviceId)this.GetPrimaryKey();

        foreach (var registration in registrations)
        {
            foreach (var tenantId in _tenants.GetTenantIds())
            {
                _logger.RegisterObserver(
                    registration.ObserverId,
                    registration.Name,
                    registration.EventSequenceId);
                var key = new ObserverKey(microserviceId, tenantId, registration.EventSequenceId);
                var observer = GrainFactory.GetGrain<IClientObserver>(registration.ObserverId, key);
                await observer.Start(registration.Name, connectionId, registration.EventTypes);
            }
        }
    }
}
