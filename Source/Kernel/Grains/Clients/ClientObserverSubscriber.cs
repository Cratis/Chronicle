// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using System.Text.Json;
using Aksio.Cratis.Clients;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientObserverSubscriber"/>.
/// </summary>
public class ClientObserverSubscriber : Grain, IClientObserverSubscriber
{
    readonly List<ConnectedClient> _clients = new();
    readonly ILogger<ClientObserverSubscriber> _logger;
    readonly IHttpClientFactory _httpClientFactory;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    MicroserviceId _microserviceId = MicroserviceId.Unspecified;
    ObserverId _observerId = ObserverId.Unspecified;
    TenantId _tenantId = TenantId.NotSet;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;
    IConnectedClients? _connectedClients;
    IConnectedClients ConnectedClients => _connectedClients ??= GrainFactory.GetGrain<IConnectedClients>(_microserviceId);

    public ClientObserverSubscriber(
        ILogger<ClientObserverSubscriber> logger,
        IHttpClientFactory httpClientFactory,
        JsonSerializerOptions jsonSerializerOptions)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    /// <inheritdoc/>
    public override Task OnActivateAsync()
    {
        var id = this.GetPrimaryKey(out var keyAsString);
        var key = ObserverSubscriberKey.Parse(keyAsString);
        _microserviceId = key.MicroserviceId;
        _observerId = id;
        _tenantId = key.TenantId;
        _eventSequenceId = key.EventSequenceId;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task OnNext(AppendedEvent @event)
    {
        _logger.EventReceived(
            _observerId,
            _microserviceId,
            _tenantId,
            @event.Metadata.Type.Id,
            _eventSequenceId,
            @event.Context.SequenceNumber);

        var clients = await ConnectedClients.GetAllConnectedClients();
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = clients.First().ClientUri;

        var jsonContent = JsonContent.Create(@event, options: _jsonSerializerOptions);
        await client.PostAsync($"/.aksio/observers/{_observerId}", jsonContent);
    }
}
