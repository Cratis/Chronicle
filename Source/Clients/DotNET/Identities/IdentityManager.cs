// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Identities;

namespace Cratis.Chronicle.Identities;

/// <summary>
/// Represents an implementation of <see cref="IIdentityManager"/> that proxies to the Chronicle kernel over the client connection.
/// </summary>
/// <param name="eventStore">The <see cref="EventStoreName"/> the manager operates within.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the manager operates within.</param>
/// <param name="connection">The <see cref="IChronicleConnection"/> for talking to the kernel.</param>
public class IdentityManager(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    IChronicleConnection connection) : IIdentityManager
{
    readonly IChronicleServicesAccessor _servicesAccessor = (connection as IChronicleServicesAccessor)!;

    /// <inheritdoc/>
    public Task Rename(string subject, string name) =>
        _servicesAccessor.Services.Identities.RenameIdentity(new RenameIdentityRequest
        {
            EventStore = eventStore,
            Namespace = @namespace,
            Subject = subject,
            Name = name
        });
}
