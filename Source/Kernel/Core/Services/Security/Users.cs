// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Storage;
using Cratis.Reactive;
using Microsoft.Extensions.Options;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Security;

/// <summary>
/// Represents an implementation of <see cref="IUsers"/>.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="storage">The <see cref="IStorage"/> for working with users.</param>
/// <param name="options">The Chronicle options.</param>
/// <param name="eventSerializer">The event serializer.</param>
/// <param name="observers">The observer service used to await user projection completion.</param>
internal sealed class Users(
    IGrainFactory grainFactory,
    IStorage storage,
    IOptions<Configuration.ChronicleOptions> options,
    Cratis.Chronicle.EventSequences.IEventSerializer eventSerializer,
    IObservers observers) : IUsers
{
    /// <inheritdoc/>
    public Task<CommandResult> AddUser(AddUserRequest request, CallContext callContext = default) =>
        CommandExecutor.Execute(
            new Chronicle.Security.AddUser(request.UserId, request.Username, request.Email, request.Password),
            command => command.Handle(grainFactory, storage));

    /// <inheritdoc/>
    public async Task<CommandResult> ChangeUserPassword(ChangeUserPasswordRequest request, CallContext callContext = default)
    {
        var tailSequenceNumber = EventSequenceNumber.Unavailable;
        var result = await CommandExecutor.Execute(
            new Chronicle.Security.ChangeUserPassword(request.UserId, request.OldPassword, request.Password, request.ConfirmedPassword),
            async command => tailSequenceNumber = await command.Handle(grainFactory, storage));
        return await WaitForUserProjection(result, tailSequenceNumber, callContext);
    }

    /// <inheritdoc/>
    public Task<CommandResult> RemoveUser(RemoveUserRequest request, CallContext callContext = default) =>
        CommandExecutor.Execute(
            new Chronicle.Security.RemoveUser(request.UserId),
            command => command.Handle(grainFactory));

    /// <inheritdoc/>
    public Task<CommandResult> RequirePasswordChange(RequirePasswordChangeRequest request, CallContext callContext = default) =>
        CommandExecutor.Execute(
            new Chronicle.Security.RequirePasswordChange(request.UserId),
            command => command.Handle(grainFactory));

    /// <inheritdoc/>
    public async Task<CommandResult> SetInitialAdminPassword(SetInitialAdminPasswordRequest request, CallContext callContext = default)
    {
        var tailSequenceNumber = EventSequenceNumber.Unavailable;
        var result = await CommandExecutor.Execute(
            new Chronicle.Security.SetInitialAdminPassword(request.UserId, request.Password, request.ConfirmedPassword),
            async command => tailSequenceNumber = await command.Handle(grainFactory, storage, options.Value.Authentication, eventSerializer));
        return await WaitForUserProjection(result, tailSequenceNumber, callContext);
    }

    /// <inheritdoc/>
    public Task<QueryResult<AdminPasswordStatusResponse>> GetStatus(CallContext callContext = default) =>
        QueryExecutor.Execute(async () =>
        {
            var status = await Chronicle.Security.AdminPasswordStatus.GetStatus(storage, options);
            return new AdminPasswordStatusResponse
            {
                IsRequired = status.IsRequired,
                AdminUserId = status.AdminUserId,
                AdminUsername = status.AdminUsername
            };
        });

    /// <inheritdoc/>
    public IObservable<QueryResult<IEnumerable<UserResponse>>> AllUsers(CallContext callContext = default) =>
        QueryExecutor.Execute(() =>
            Chronicle.Security.User.AllUsers(storage)
                .CompletedBy(callContext.CancellationToken)
                .Select(users => (IEnumerable<UserResponse>)users.Select<Chronicle.Security.User, UserResponse>(u => ToResponse(u)).ToList()));

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

    async Task<CommandResult> WaitForUserProjection(
        CommandResult result,
        EventSequenceNumber tailSequenceNumber,
        CallContext callContext)
    {
        if (!result.IsSuccess || !tailSequenceNumber.IsActualValue)
        {
            return result;
        }

        var completion = await observers.WaitForCompletion(
            new WaitForObserverCompletionRequest
            {
                EventStore = EventStoreName.System,
                Namespace = EventStoreNamespaceName.Default,
                EventSequenceId = EventSequenceId.Log,
                TailEventSequenceNumber = tailSequenceNumber
            },
            callContext);

        return completion.IsSuccess
            ? result
            : CommandResult.Error(result.CorrelationId, new UserProjectionDidNotComplete());
    }
}
