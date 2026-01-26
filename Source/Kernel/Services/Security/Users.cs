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

        var user = await storage.System.Users.GetById(command.UserId) ?? throw new UserNotFound(command.UserId);

        if (user.PasswordHash is null || !HashHelper.Verify(command.OldPassword, user.PasswordHash))
        {
            throw new InvalidOldPassword();
        }

        if (HashHelper.Verify(command.Password, user.PasswordHash))
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
    public async Task RequirePasswordChange(RequirePasswordChange command)
    {
        var @event = new PasswordChangeRequired();
        var eventSequence = grainFactory.GetEventLog();

        await eventSequence.Append(
            command.UserId,
            @event);
    }

    /// <inheritdoc/>
    public async Task SetInitialAdminPassword(SetInitialAdminPassword command)
    {
        if (command.Password != command.ConfirmedPassword)
        {
            throw new PasswordConfirmationMismatch();
        }

        var user = await storage.System.Users.GetById(command.UserId) ?? throw new UserNotFound(command.UserId);

        // Ensure this is only for users who haven't logged in yet
        if (user.HasLoggedIn)
        {
            throw new InvalidOperationException("Setting initial admin password is only allowed for users who haven't set their initial password.");
        }

        var passwordHash = HashHelper.Hash(command.Password);

        var @event = new UserPasswordChanged(passwordHash);

        var eventSequence = grainFactory.GetEventLog();

        await eventSequence.Append(
            command.UserId,
            @event);
    }

    /// <inheritdoc/>
    public async Task<IList<User>> GetAll()
    {
        var users = await storage.System.Users.GetAll();
        return users.Select(ToContract).ToList();
    }

    /// <inheritdoc/>
    public IObservable<IList<User>> ObserveAll(CallContext context = default) =>
        storage.System.Users
            .ObserveAll()
            .CompletedBy(context.CancellationToken)
            .Select(users => users.Select(ToContract).ToList());

    /// <inheritdoc/>
    public async Task<InitialAdminPasswordSetupStatus> GetInitialAdminPasswordSetupStatus()
    {
        var users = await storage.System.Users.GetAll();
        var adminUser = users.FirstOrDefault(u => u.Username == "admin" && !u.HasLoggedIn);

        return new InitialAdminPasswordSetupStatus
        {
            IsRequired = adminUser is not null,
            AdminUserId = adminUser is not null ? (Guid)adminUser.Id : null
        };
    }

    static User ToContract(ChronicleUser user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email ?? string.Empty,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt!,
        LastModifiedAt = user.LastModifiedAt,
        HasLoggedIn = user.HasLoggedIn
    };
}
