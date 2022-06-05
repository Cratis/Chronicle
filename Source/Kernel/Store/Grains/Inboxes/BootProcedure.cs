// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Boot;
using Aksio.Cratis.Configuration;
using Aksio.Cratis.Events.Store.Inboxes;
using Aksio.Cratis.Execution;
using Orleans;

namespace Aksio.Cratis.Events.Store.Grains.Inboxes;

/// <summary>
/// Represents a boot procedure for starting all inboxes.
/// </summary>
public class BootProcedure : IPerformBootProcedure
{
    readonly IGrainFactory _grainFactory;
    readonly KernelConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="BootProcedure"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    /// <param name="configuration">The <see cref="KernelConfiguration"/>.</param>
    public BootProcedure(
        IGrainFactory grainFactory,
        KernelConfiguration configuration)
    {
        _grainFactory = grainFactory;
        _configuration = configuration;
    }

    /// <inheritdoc/>
    public void Perform()
    {
        foreach (var (microserviceId, microservice) in _configuration.Microservices)
        {
            foreach (var outbox in microservice.Inbox.FromOutboxes)
            {
                foreach (var (tenantId, _) in _configuration.Tenants)
                {
                    var key = new InboxKey(tenantId, outbox.Microservice);
                    var inbox = _grainFactory.GetGrain<IInbox>((MicroserviceId)microserviceId, key);
                    inbox.Start().Wait();
                }
            }
        }
    }
}
