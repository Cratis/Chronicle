// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Orleans.Runtime.Services;

namespace Cratis.Chronicle.Grains.Observation.Webhooks;

/// <summary>
/// Represents an implementation of <see cref="WebhooksServiceClient"/>.
/// </summary>
/// <param name="grainFactory"><see cref="IGrainFactory"/> to use for getting grains.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for getting services.</param>
public class WebhooksServiceClient(IGrainFactory grainFactory, IServiceProvider serviceProvider)
    : GrainServiceClient<IWebhooksService>(serviceProvider), IWebhooksServiceClient
{
    readonly IManagementGrain _managementGrain = grainFactory.GetGrain<IManagementGrain>(1);

    /// <inheritdoc/>
    public async Task Register(EventStoreName eventStore, IEnumerable<WebhookDefinition> definitions)
    {
        var hosts = await _managementGrain.GetHosts(true);
        foreach (var host in hosts.Keys)
        {
            await GetGrainService(host).Register(eventStore, definitions);
        }
    }

    /// <inheritdoc/>
    public async Task NamespaceAdded(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        var hosts = await _managementGrain.GetHosts(true);
        foreach (var host in hosts.Keys)
        {
            await GetGrainService(host).NamespaceAdded(eventStore, @namespace);
        }
    }
}
