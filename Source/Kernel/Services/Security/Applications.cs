// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Chronicle.Grains.EventSequences;
using Cratis.Chronicle.Grains.Security;
using Cratis.Chronicle.Storage;
using Cratis.Reactive;
using Microsoft.AspNetCore.Identity;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Services.Security;

/// <summary>
/// Represents an implementation of <see cref="IApplications"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Applications"/> class.
/// </remarks>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for creating grains.</param>
/// <param name="storage">The <see cref="IStorage"/> for working with applications.</param>
internal sealed class Applications(IGrainFactory grainFactory, IStorage storage) : IApplications
{
    /// <inheritdoc/>
    static readonly PasswordHasher<object> _passwordHasher = new();

    /// <inheritdoc/>
    public async Task Add(AddApplication command)
    {
        var clientSecret = _passwordHasher.HashPassword(null!, command.ClientSecret);

        var @event = new ApplicationAdded(
            command.ClientId,
            clientSecret);

        var eventSequence = grainFactory.GetEventLog();
        await eventSequence.Append(
            command.Id,
            @event);
    }

    /// <inheritdoc/>
    public async Task Remove(RemoveApplication command)
    {
        var @event = new ApplicationRemoved();

        var eventSequence = grainFactory.GetEventLog();

        await eventSequence.Append(
            command.Id,
            @event);
    }

    /// <inheritdoc/>
    public async Task ChangeSecret(ChangeApplicationSecret command)
    {
        var clientSecret = _passwordHasher.HashPassword(null!, command.ClientSecret);

        var @event = new ApplicationSecretChanged(clientSecret);

        var eventSequence = grainFactory.GetEventLog();
        await eventSequence.Append(
            command.Id,
            @event);
    }

    /// <inheritdoc/>
    public async Task<IList<Application>> GetAll()
    {
        var clients = await storage.System.Applications.GetAll();
        return clients.Select(ToContract).ToList();
    }

    /// <inheritdoc/>
    public IObservable<IList<Application>> ObserveAll(CallContext context = default) =>
        storage.System.Applications
            .ObserveAll()
            .CompletedBy(context.CancellationToken)
            .Select(apps => apps.Select(ToContract).ToList());

    static Application ToContract(Storage.Security.Application client) => new()
    {
        Id = client.Id,
        ClientId = client.ClientId,
        IsActive = true,
        CreatedAt = DateTimeOffset.UtcNow!,
        LastModifiedAt = null
    };
}
