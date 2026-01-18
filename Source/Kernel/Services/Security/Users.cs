// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Grains.EventSequences;
using Cratis.Chronicle.Grains.Security;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Security;
using Cratis.Infrastructure.Security;
using Cratis.Reactive;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Security;

/// <summary>
/// Represents an implementation of <see cref="IUsers"/>.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="storage">The <see cref="IStorage"/> for working with users.</param>
internal sealed class Users(
    IGrainFactory grainFactory,
    IStorage storage) : IUsers
{
    /// <inheritdoc/>
    public async Task Add(AddUser command)
    {
        var passwordHash = HashHelper.Hash(command.Password);

        var @event = new UserAdded(
            command.Username,
            command.Email,
            passwordHash);

        var eventSequence = grainFactory.GetEventLog();
        await eventSequence.Append(command.UserId, @event);
    }

    /// <inheritdoc/>
    public async Task Remove(RemoveUser command)
    {
        var @event = new UserRemoved();
        var eventSequence = grainFactory.GetEventLog();

        await eventSequence.Append(
            command.UserId,
            @event);
    }

    /// <inheritdoc/>
    public async Task ChangePassword(ChangeUserPassword command)
    {
        if (command.Password != command.ConfirmedPassword)
        {
            throw new PasswordConfirmationMismatch();
        }

        var user = await storage.System.Users.GetById(command.UserId);
        if (user is null)
        {
            throw new UserNotFound(command.UserId);
        }

        if (user.PasswordHash is not null && HashHelper.Verify(command.Password, user.PasswordHash))
        {
            throw new NewPasswordMustBeDifferent();
        }

        var passwordHash = HashHelper.Hash(command.Password);

        var @event = new UserPasswordChanged(passwordHash);

        var eventSequence = grainFactory.GetEventLog();

        await eventSequence.Append(
            command.UserId,
            @event);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<User>> GetAll()
    {
        var users = await storage.System.Users.GetAll();
        return users.Select(ToContract);
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<User>> ObserveAll(CallContext context = default) =>
        storage.System.Users
            .ObserveAll()
            .CompletedBy(context.CancellationToken)
            .Select(users => users.Select(ToContract).ToArray());

    static User ToContract(ChronicleUser user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email ?? string.Empty,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        LastModifiedAt = user.LastModifiedAt
    };
}
