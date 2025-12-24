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
using Cratis.Reactive;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Security;

/// <summary>
/// Represents an implementation of <see cref="IUsers"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Users"/> class.
/// </remarks>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="userStorage">The <see cref="IUserStorage"/> for working with users.</param>
internal sealed class Users(IGrainFactory grainFactory, IUserStorage userStorage) : IUsers
{
    /// <inheritdoc/>
    public async Task Add(AddUser command)
    {
        var passwordHash = HashHelper.Hash(command.Password);

        var @event = new UserAdded(
            command.UserId,
            command.Username,
            command.Email,
            passwordHash);

        var eventSequence = grainFactory.GetSystemEventSequence();
        var jsonObject = (JsonObject)JsonSerializer.SerializeToNode(@event)!;

        await eventSequence.AppendMany(
            [
                new EventToAppend(
                    EventSourceType.Default,
                    command.UserId,
                    EventStreamType.All,
                    EventStreamId.Default,
                    typeof(UserAdded).GetEventType(),
                    jsonObject)
            ],
            CorrelationId.New(),
            [],
            Identity.System,
            new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));
    }

    /// <inheritdoc/>
    public async Task Remove(RemoveUser command)
    {
        var @event = new UserRemoved(command.UserId);

        var eventSequence = grainFactory.GetSystemEventSequence();
        var jsonObject = (JsonObject)JsonSerializer.SerializeToNode(@event)!;

        await eventSequence.AppendMany(
            [
                new EventToAppend(
                    EventSourceType.Default,
                    command.UserId,
                    EventStreamType.All,
                    EventStreamId.Default,
                    typeof(UserRemoved).GetEventType(),
                    jsonObject)
            ],
            CorrelationId.New(),
            [],
            Identity.System,
            new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));
    }

    /// <inheritdoc/>
    public async Task ChangePassword(ChangeUserPassword command)
    {
        var passwordHash = HashHelper.Hash(command.Password);

        var @event = new UserPasswordChanged(command.UserId, passwordHash);

        var eventSequence = grainFactory.GetSystemEventSequence();
        var jsonObject = (JsonObject)JsonSerializer.SerializeToNode(@event)!;

        await eventSequence.AppendMany(
            [
                new EventToAppend(
                    EventSourceType.Default,
                    command.UserId,
                    EventStreamType.All,
                    EventStreamId.Default,
                    typeof(UserPasswordChanged).GetEventType(),
                    jsonObject)
            ],
            CorrelationId.New(),
            [],
            Identity.System,
            new ConcurrencyScopes(new Dictionary<EventSourceId, ConcurrencyScope>()));
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<User>> GetAll()
    {
        var users = await userStorage.GetAll();
        return users.Select(ToContract);
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<User>> ObserveAll(CallContext context = default) =>
        userStorage
            .ObserveAll()
            .CompletedBy(context.CancellationToken)
            .Select(users => users.Select(ToContract).ToArray());

    static User ToContract(ChronicleUser user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        LastModifiedAt = user.LastModifiedAt
    };
}
