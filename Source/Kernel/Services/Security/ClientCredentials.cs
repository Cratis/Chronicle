// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Grains.EventSequences;
using Cratis.Chronicle.Grains.Security;
using Cratis.Chronicle.Storage.Security;
using Cratis.Infrastructure.Security;
using Cratis.Reactive;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Security;

/// <summary>
/// Represents an implementation of <see cref="IClientCredentials"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ClientCredentials"/> class.
/// </remarks>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="applicationStorage">The <see cref="IApplicationStorage"/> for working with client credentials.</param>
internal sealed class ClientCredentials(IGrainFactory grainFactory, IApplicationStorage applicationStorage) : IClientCredentials
{
    /// <inheritdoc/>
    public async Task Add(AddClientCredentials command)
    {
        var clientSecret = HashHelper.Hash(command.ClientSecret);
        
        var @event = new ClientCredentialsAdded(
            command.Id,
            command.ClientId,
            clientSecret);

        var eventSequence = grainFactory.GetSystemEventSequence();
        var jsonObject = (JsonObject)JsonSerializer.SerializeToNode(@event)!;

        await eventSequence.AppendMany(
            [
                new EventToAppend(
                    EventSourceType.Default,
                    command.Id,
                    EventStreamType.All,
                    EventStreamId.Default,
                    typeof(ClientCredentialsAdded).GetEventType(),
                    jsonObject)
            ],
            CorrelationId.New(),
            [],
            Identity.System,
            new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));
    }

    /// <inheritdoc/>
    public async Task Remove(RemoveClientCredentials command)
    {
        var @event = new ClientCredentialsRemoved(command.Id);
        
        var eventSequence = grainFactory.GetSystemEventSequence();
        var jsonObject = (JsonObject)JsonSerializer.SerializeToNode(@event)!;

        await eventSequence.AppendMany(
            [
                new EventToAppend(
                    EventSourceType.Default,
                    command.Id,
                    EventStreamType.All,
                    EventStreamId.Default,
                    typeof(ClientCredentialsRemoved).GetEventType(),
                    jsonObject)
            ],
            CorrelationId.New(),
            [],
            Identity.System,
            new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));
    }

    /// <inheritdoc/>
    public async Task ChangeSecret(ChangeClientCredentialsSecret command)
    {
        var clientSecret = HashHelper.Hash(command.ClientSecret);
        
        var @event = new ClientCredentialsSecretChanged(command.Id, clientSecret);
        
        var eventSequence = grainFactory.GetSystemEventSequence();
        var jsonObject = (JsonObject)JsonSerializer.SerializeToNode(@event)!;

        await eventSequence.AppendMany(
            [
                new EventToAppend(
                    EventSourceType.Default,
                    command.Id,
                    EventStreamType.All,
                    EventStreamId.Default,
                    typeof(ClientCredentialsSecretChanged).GetEventType(),
                    jsonObject)
            ],
            CorrelationId.New(),
            [],
            Identity.System,
            new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Contracts.Security.ClientCredentials>> GetAll()
    {
        var clients = await applicationStorage.GetAll();
        return clients.Select(ToContract);
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<Contracts.Security.ClientCredentials>> ObserveAll(CallContext context = default)
    {
        // Since IApplicationStorage doesn't have ObserveAll, we'll return an empty observable for now
        // This can be enhanced by adding observable support to IApplicationStorage if needed
        return System.Reactive.Linq.Observable.Empty<IEnumerable<Contracts.Security.ClientCredentials>>();
    }

    static Contracts.Security.ClientCredentials ToContract(Application client) => new()
    {
        Id = client.Id,
        ClientId = client.ClientId,
        IsActive = true,
        CreatedAt = DateTimeOffset.UtcNow,
        LastModifiedAt = null
    };
}
