// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Storage;
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
    public Task AddUser(AddUserRequest request, CallContext callContext = default) =>
        new Chronicle.Security.AddUser(request.UserId, request.Username, request.Email, request.Password)
            .Handle(grainFactory);

    /// <inheritdoc/>
    public Task ChangeUserPassword(ChangeUserPasswordRequest request, CallContext callContext = default) =>
        new Chronicle.Security.ChangeUserPassword(request.UserId, request.OldPassword, request.Password, request.ConfirmedPassword)
            .Handle(grainFactory, storage);

    /// <inheritdoc/>
    public Task RemoveUser(RemoveUserRequest request, CallContext callContext = default) =>
        new Chronicle.Security.RemoveUser(request.UserId)
            .Handle(grainFactory);

    /// <inheritdoc/>
    public Task RequirePasswordChange(RequirePasswordChangeRequest request, CallContext callContext = default) =>
        new Chronicle.Security.RequirePasswordChange(request.UserId)
            .Handle(grainFactory);

    /// <inheritdoc/>
    public Task SetInitialAdminPassword(SetInitialAdminPasswordRequest request, CallContext callContext = default) =>
        new Chronicle.Security.SetInitialAdminPassword(request.UserId, request.Password, request.ConfirmedPassword)
            .Handle(grainFactory, storage);

    /// <inheritdoc/>
    public async Task<AdminPasswordStatusResponse> GetStatus(CallContext callContext = default)
    {
        var status = await Chronicle.Security.AdminPasswordStatus.GetStatus(storage);
        return new AdminPasswordStatusResponse
        {
            IsRequired = status.IsRequired,
            AdminUserId = status.AdminUserId
        };
    }

    /// <inheritdoc/>
    public IObservable<IEnumerable<UserResponse>> AllUsers(CallContext callContext = default) =>
        Chronicle.Security.User.AllUsers(storage)
            .CompletedBy(callContext.CancellationToken)
            .Select(users => (IEnumerable<UserResponse>)users.Select<Chronicle.Security.User, UserResponse>(u => ToResponse(u)).ToList());

    static UserResponse ToResponse(Chronicle.Security.User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email ?? string.Empty,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        LastModifiedAt = user.LastModifiedAt,
        HasLoggedIn = user.HasLoggedIn
    };
}
