// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Reducers;
using Microsoft.Extensions.Logging;
using TenantsConfig = Aksio.Cratis.Kernel.Configuration.Tenants;

namespace Aksio.Cratis.Kernel.Grains.Observation.Reducers.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientReducers"/>.
/// </summary>
public class ClientReducers : Grain, IClientReducers
{
    readonly TenantsConfig _tenants;
    readonly ILogger<ClientReducers> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientReducers"/> class.
    /// </summary>
    /// <param name="tenants">The configured <see cref="TenantsConfig"/>.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ClientReducers(
        TenantsConfig tenants,
        ILogger<ClientReducers> logger)
    {
        _tenants = tenants;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task Register(ConnectionId connectionId, IEnumerable<ClientReducerRegistration> registrations)
    {
        _logger.RegisterReducers();

        var microserviceId = (MicroserviceId)this.GetPrimaryKey();

        foreach (var registration in registrations)
        {
            foreach (var tenantId in _tenants.GetTenantIds())
            {
                _logger.RegisterReducer(
                    registration.ReducerId,
                    registration.Name,
                    registration.EventSequenceId);
                var key = new ObserverKey(microserviceId, tenantId, registration.EventSequenceId);
                var reducer = GrainFactory.GetGrain<IClientReducer>(registration.ReducerId, key);
                await reducer.Start(registration.Name, connectionId, registration.EventTypes);
            }
        }
    }
}
