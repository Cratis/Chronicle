// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR;

/// <summary>
/// Represents an implementation of <see cref="IPIIManager"/> that proxies to the Chronicle kernel over the client connection.
/// </summary>
/// <param name="eventStore">The <see cref="EventStoreName"/> the manager operates within.</param>
/// <param name="namespace">The <see cref="EventStoreNamespaceName"/> the manager operates within.</param>
/// <param name="connection">The <see cref="IChronicleConnection"/> for talking to the kernel.</param>
public class PIIManager(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    IChronicleConnection connection) : IPIIManager
{
    readonly IChronicleServicesAccessor _servicesAccessor = (connection as IChronicleServicesAccessor)!;

    /// <inheritdoc/>
    public Task DeleteEncryptionKeyFor(EncryptionKeyIdentifier identifier) =>
        _servicesAccessor.Services.Compliance.DeleteEncryptionKey(new DeleteEncryptionKeyRequest
        {
            EventStore = eventStore,
            Namespace = @namespace,
            Identifier = identifier
        });
}
