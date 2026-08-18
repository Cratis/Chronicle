// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Storage;
using Cratis.Reactive;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Security;

/// <summary>
/// Represents an implementation of <see cref="IApplications"/>.
/// </summary>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="storage">The <see cref="IStorage"/> for working with applications.</param>
internal sealed class Applications(IGrainFactory grainFactory, IStorage storage) : IApplications
{
    /// <inheritdoc/>
    public Task<CommandResult> AddApplication(AddApplicationRequest request, CallContext callContext = default) =>
        CommandExecutor.Execute(
            new Chronicle.Security.AddApplication(request.Id, request.ClientId, request.ClientSecret),
            command => command.Handle(grainFactory, storage));

    /// <inheritdoc/>
    public Task<CommandResult> ChangeApplicationSecret(ChangeApplicationSecretRequest request, CallContext callContext = default) =>
        CommandExecutor.Execute(
            new Chronicle.Security.ChangeApplicationSecret(request.Id, request.ClientSecret),
            command => command.Handle(grainFactory));

    /// <inheritdoc/>
    public Task<CommandResult> RemoveApplication(RemoveApplicationRequest request, CallContext callContext = default) =>
        CommandExecutor.Execute(
            new Chronicle.Security.RemoveApplication(request.Id),
            command => command.Handle(grainFactory));

    /// <inheritdoc/>
    public IObservable<QueryResult<IEnumerable<ApplicationResponse>>> AllApplications(CallContext callContext = default) =>
        QueryExecutor.Execute(() =>
            Chronicle.Security.Application.AllApplications(storage)
                .CompletedBy(callContext.CancellationToken)
                .Select(apps => (IEnumerable<ApplicationResponse>)apps.Select(a => ToResponse(a)).ToList()));

    static ApplicationResponse ToResponse(Chronicle.Security.Application app) => new()
    {
        Id = app.Id,
        ClientId = app.ClientId,
        IsActive = app.IsActive,
        CreatedAt = app.CreatedAt,
        LastModifiedAt = app.LastModifiedAt
    };
}
