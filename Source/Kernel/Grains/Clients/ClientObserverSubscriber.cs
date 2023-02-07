// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Aksio.Cratis.Clients;
using Aksio.Cratis.Commands;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Observation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Aksio.Cratis.Kernel.Grains.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientObserverSubscriber"/>.
/// </summary>
public class ClientObserverSubscriber : Grain, IClientObserverSubscriber
{
    readonly ILogger<ClientObserverSubscriber> _logger;
    readonly IHttpClientFactory _httpClientFactory;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    MicroserviceId _microserviceId = MicroserviceId.Unspecified;
    ObserverId _observerId = ObserverId.Unspecified;
    TenantId _tenantId = TenantId.NotSet;
    EventSequenceId _eventSequenceId = EventSequenceId.Unspecified;
    IConnectedClients? _connectedClients;
    IConnectedClients ConnectedClients => _connectedClients ??= GrainFactory.GetGrain<IConnectedClients>(_microserviceId);
    IEnumerable<ConnectedClient> _clients = Enumerable.Empty<ConnectedClient>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientObserverSubscriber"/> class.
    /// </summary>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    /// <param name="httpClientFactory"><see cref="IHttpClientFactory"/> for connecting to the client.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
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
    public async Task<ObserverSubscriberResult> OnNext(AppendedEvent @event)
    {
        _logger.EventReceived(
            _observerId,
            _microserviceId,
            _tenantId,
            @event.Metadata.Type.Id,
            _eventSequenceId,
            @event.Context.SequenceNumber);

        if (!_clients.Any())
        {
            _clients = await ConnectedClients.GetAllConnectedClients();
        }

        var first = _clients.FirstOrDefault();
        if (first is not null)
        {
            using var client = _httpClientFactory.CreateClient(Clients.ConnectedClients.ConnectedClientsHttpClient);
            client.BaseAddress = first.ClientUri;

            var jsonContent = JsonContent.Create(@event, options: _jsonSerializerOptions);
            client.DefaultRequestHeaders.Add(ExecutionContextAppBuilderExtensions.TenantIdHeader, _tenantId.ToString());
            var response = await client.PostAsync($"/.cratis/observers/{_observerId}", jsonContent);
            var commandResult = (await response.Content.ReadFromJsonAsync<CommandResult>(_jsonSerializerOptions))!;
            var state = ObserverSubscriberState.Ok;

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                await ConnectedClients.OnClientDisconnected(first.ConnectionId, "Client not found");
                _clients = Enumerable.Empty<ConnectedClient>();
                state = ObserverSubscriberState.Disconnected;
            }
            else if (response.StatusCode != HttpStatusCode.OK || !commandResult.IsSuccess)
            {
                state = ObserverSubscriberState.Failed;
            }

            return new ObserverSubscriberResult(state, commandResult.ExceptionMessages, commandResult.ExceptionStackTrace);
        }

        return new ObserverSubscriberResult(ObserverSubscriberState.Disconnected, Enumerable.Empty<string>(), string.Empty);
    }
}
