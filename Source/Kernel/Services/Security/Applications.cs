// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Grains.EventSequences;
using Cratis.Chronicle.Grains.Security;
using Cratis.Chronicle.Storage.Security;
using Cratis.Infrastructure.Security;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Security;

/// <summary>
/// Represents an implementation of <see cref="IApplications"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Applications"/> class.
/// </remarks>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="applicationStorage">The <see cref="IApplicationStorage"/> for working with applications.</param>
internal sealed class Applications(IGrainFactory grainFactory, IApplicationStorage applicationStorage) : IApplications
{
    /// <inheritdoc/>
    public async Task Add(AddApplication command)
    {
        var clientSecret = HashHelper.Hash(command.ClientSecret);

        var @event = new ApplicationAdded(
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
                    typeof(ApplicationAdded).GetEventType(),
                    jsonObject)
            ],
            CorrelationId.New(),
            [],
            Identity.System,
            new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));
    }

    /// <inheritdoc/>
    public async Task Remove(RemoveApplication command)
    {
        var @event = new ApplicationRemoved(command.Id);

        var eventSequence = grainFactory.GetSystemEventSequence();
        var jsonObject = (JsonObject)JsonSerializer.SerializeToNode(@event)!;

        await eventSequence.AppendMany(
            [
                new EventToAppend(
                    EventSourceType.Default,
                    command.Id,
                    EventStreamType.All,
                    EventStreamId.Default,
                    typeof(ApplicationRemoved).GetEventType(),
                    jsonObject)
            ],
            CorrelationId.New(),
            [],
            Identity.System,
            new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));
    }

    /// <inheritdoc/>
    public async Task ChangeSecret(ChangeApplicationSecret command)
    {
        var clientSecret = HashHelper.Hash(command.ClientSecret);

        var @event = new ApplicationSecretChanged(command.Id, clientSecret);

        var eventSequence = grainFactory.GetSystemEventSequence();
        var jsonObject = (JsonObject)JsonSerializer.SerializeToNode(@event)!;

        await eventSequence.AppendMany(
            [
                new EventToAppend(
                    EventSourceType.Default,
                    command.Id,
                    EventStreamType.All,
                    EventStreamId.Default,
                    typeof(ApplicationSecretChanged).GetEventType(),
                    jsonObject)
            ],
            CorrelationId.New(),
            [],
            Identity.System,
            new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Contracts.Security.Application>> GetAll()
    {
        var clients = await applicationStorage.GetAll();
        return clients.Select(ToContract);
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<Contracts.Security.Application>> ObserveAll(CallContext context = default)
    {
        // Since IApplicationStorage doesn't have ObserveAll, we'll return an empty observable for now
        // This can be enhanced by adding observable support to IApplicationStorage if needed
        return System.Reactive.Linq.Observable.Empty<IEnumerable<Contracts.Security.Application>>();
    }

    static Contracts.Security.Application ToContract(Storage.Security.Application client) => new()
    {
        Id = client.Id,
        ClientId = client.ClientId,
        IsActive = true,
        CreatedAt = DateTimeOffset.UtcNow,
        LastModifiedAt = null
    };
}
