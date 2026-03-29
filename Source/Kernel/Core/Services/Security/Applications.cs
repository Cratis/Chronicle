// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    public Task AddApplication(AddApplicationRequest request, CallContext callContext = default) =>
        new Chronicle.Security.AddApplication(request.Id, request.ClientId, request.ClientSecret)
            .Handle(grainFactory);

    /// <inheritdoc/>
    public Task ChangeApplicationSecret(ChangeApplicationSecretRequest request, CallContext callContext = default) =>
        new Chronicle.Security.ChangeApplicationSecret(request.Id, request.ClientSecret)
            .Handle(grainFactory);

    /// <inheritdoc/>
    public Task RemoveApplication(RemoveApplicationRequest request, CallContext callContext = default) =>
        new Chronicle.Security.RemoveApplication(request.Id)
            .Handle(grainFactory);

    /// <inheritdoc/>
    public IObservable<IEnumerable<ApplicationResponse>> AllApplications(CallContext callContext = default) =>
        Chronicle.Security.Application.AllApplications(storage)
            .CompletedBy(callContext.CancellationToken)
            .Select(apps => (IEnumerable<ApplicationResponse>)apps.Select(a => ToResponse(a)).ToList());

    static ApplicationResponse ToResponse(Chronicle.Security.Application app) => new()
    {
        Id = app.Id,
        ClientId = app.ClientId,
        IsActive = app.IsActive,
        CreatedAt = app.CreatedAt,
        LastModifiedAt = app.LastModifiedAt
    };
}
