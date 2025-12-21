// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Security.Cryptography;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Grains.EventSequences;
using Cratis.Chronicle.Grains.Security;
using Cratis.Chronicle.Storage.Security;
using Cratis.Reactive;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
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
/// <param name="causationManager">The <see cref="ICausationManager"/> for managing causation.</param>
internal sealed class Users(IGrainFactory grainFactory, IUserStorage userStorage, ICausationManager causationManager) : IUsers
{
    /// <inheritdoc/>
    public async Task Add(AddUser command)
    {
        var passwordHash = HashPassword(command.Password);
        var eventSequence = grainFactory.GetEventSequence(EventStoreName.System, EventStoreNamespaceName.Default);

        var @event = new UserAdded(
            command.UserId,
            command.Username,
            command.Email,
            passwordHash);

        await eventSequence.AppendMany(
            [
                new EventToAppend(
                    EventSourceType.NotSet,
                    command.UserId,
                    EventStreamType.NotSet,
                    EventStreamId.NotSet,
                    @event.GetType(),
                    @event)
            ],
            causationManager.GetCurrentChain(),
            [],
            causationManager.GetCurrentCausedBy(),
            []);
    }

    /// <inheritdoc/>
    public async Task Remove(RemoveUser command)
    {
        var eventSequence = grainFactory.GetEventSequence(EventStoreName.System, EventStoreNamespaceName.Default);

        var @event = new UserRemoved(command.UserId);

        await eventSequence.AppendMany(
            [
                new EventToAppend(
                    EventSourceType.NotSet,
                    command.UserId,
                    EventStreamType.NotSet,
                    EventStreamId.NotSet,
                    @event.GetType(),
                    @event)
            ],
            causationManager.GetCurrentChain(),
            [],
            causationManager.GetCurrentCausedBy(),
            []);
    }

    /// <inheritdoc/>
    public async Task ChangePassword(ChangeUserPassword command)
    {
        var passwordHash = HashPassword(command.Password);
        var eventSequence = grainFactory.GetEventSequence(EventStoreName.System, EventStoreNamespaceName.Default);

        var @event = new UserPasswordChanged(command.UserId, passwordHash);

        await eventSequence.AppendMany(
            [
                new EventToAppend(
                    EventSourceType.NotSet,
                    command.UserId,
                    EventStreamType.NotSet,
                    EventStreamId.NotSet,
                    @event.GetType(),
                    @event)
            ],
            causationManager.GetCurrentChain(),
            [],
            causationManager.GetCurrentCausedBy(),
            []);
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

    static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(128 / 8);
        var hashed = KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8);

        return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hashed);
    }

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
