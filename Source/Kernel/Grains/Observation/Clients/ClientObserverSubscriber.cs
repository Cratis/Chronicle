// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Connections;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Grains.Clients;
using Aksio.Cratis.Kernel.Grains.Observation.Placement;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Observation.Clients;

/// <summary>
/// Represents an implementation of <see cref="IClientObserverSubscriber"/>.
/// </summary>
/// <remarks>
/// We want this grain to be local to its activation. When a client connects, the service instance that
/// receives the connection will activate this grain and we then want it to be local to that service instance
/// and not perform a network hop. The <see cref="ObserverKey"/> contains a <see cref="ConnectionId"/> which
/// will make the observer unique per connection, helping us to achieve this.
/// </remarks>
[ConnectedObserverPlacement]
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

    IConnectedClients ConnectedClients => _connectedClients ??= GrainFactory.GetGrain<IConnectedClients>(0);

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
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
    public async Task<ObserverSubscriberResult> OnNext(IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        /*
        foreach (var @event in events)
        {
            _logger.EventReceived(
                _observerId,
                _microserviceId,
                _tenantId,
                @event.Metadata.Type.Id,
                _eventSequenceId,
                @event.Context.SequenceNumber);
        }

        if (context.Metadata is JsonElement connectedClientJsonObject)
        {
            var connectedClient = connectedClientJsonObject.Deserialize<ConnectedClient>(_jsonSerializerOptions);
            if (connectedClient is not null)
            {
                using var httpClient = _httpClientFactory.CreateClient(Grains.Clients.ConnectedClients.ConnectedClientsHttpClient);
                httpClient.BaseAddress = connectedClient.ClientUri;

                using var jsonContent = JsonContent.Create(events, options: _jsonSerializerOptions);
                httpClient.DefaultRequestHeaders.Add(ExecutionContextAppBuilderExtensions.TenantIdHeader, _tenantId.ToString());
                var response = await httpClient.PostAsync($"/.cratis/observers/{_observerId}", jsonContent);
                var commandResult = (await response.Content.ReadFromJsonAsync<CommandResult>(_jsonSerializerOptions))!;
                var state = ObserverSubscriberState.Ok;
                var lastSuccessfullyObservedEvent = ((JsonElement)commandResult.Response!).Deserialize<EventSequenceNumber>(Globals.JsonSerializerOptions)!;

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    await ConnectedClients.OnClientDisconnected(connectedClient.ConnectionId, "Client not found");
                    state = ObserverSubscriberState.Disconnected;
                }
                else if (response.StatusCode != HttpStatusCode.OK || !commandResult.IsSuccess)
                {
                    state = ObserverSubscriberState.Failed;
                }

                return new ObserverSubscriberResult(state, lastSuccessfullyObservedEvent, commandResult.ExceptionMessages, commandResult.ExceptionStackTrace);
            }
        }

        return new ObserverSubscriberResult(ObserverSubscriberState.Disconnected, EventSequenceNumber.Unavailable, Enumerable.Empty<string>(), string.Empty);
        */

        await Task.CompletedTask;
        throw new NotImplementedException();
    }
}
