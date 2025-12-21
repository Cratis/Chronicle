// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Security;

/// <summary>
/// Defines the contract for working with client credentials.
/// </summary>
[Service]
public interface IClientCredentials
{
    /// <summary>
    /// Add new client credentials.
    /// </summary>
    /// <param name="command">The <see cref="AddClientCredentials"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Add(AddClientCredentials command);

    /// <summary>
    /// Remove client credentials.
    /// </summary>
    /// <param name="command">The <see cref="RemoveClientCredentials"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Remove(RemoveClientCredentials command);

    /// <summary>
    /// Change client credentials secret.
    /// </summary>
    /// <param name="command">The <see cref="ChangeClientCredentialsSecret"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task ChangeSecret(ChangeClientCredentialsSecret command);

    /// <summary>
    /// Gets all client credentials.
    /// </summary>
    /// <returns>Collection of <see cref="ClientCredentials"/>.</returns>
    [Operation]
    Task<IEnumerable<ClientCredentials>> GetAll();

    /// <summary>
    /// Observe all client credentials.
    /// </summary>
    /// <param name="context">The gRPC <see cref="CallContext"/>.</param>
    /// <returns>An observable of collection of <see cref="ClientCredentials"/>.</returns>
    [Operation]
    IObservable<IEnumerable<ClientCredentials>> ObserveAll(CallContext context = default);
}
