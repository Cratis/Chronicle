// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Contracts.Security;
using Cratis.Reactive;

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents the API for queries related to client credentials.
/// </summary>
[Route("/api/security/client-credentials")]
public class ClientCredentialsQueries : ControllerBase
{
    readonly IClientCredentials _clientCredentials;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientCredentialsQueries"/> class.
    /// </summary>
    /// <param name="clientCredentials">The <see cref="IClientCredentials"/> contract.</param>
    internal ClientCredentialsQueries(IClientCredentials clientCredentials)
    {
        _clientCredentials = clientCredentials;
    }

    /// <summary>
    /// Get all client credentials.
    /// </summary>
    /// <returns>A collection of client credentials.</returns>
    [HttpGet]
    public async Task<IEnumerable<ClientCredentials>> GetClientCredentials() =>
        (await _clientCredentials.GetAll()).ToApi();

    /// <summary>
    /// Observes all client credentials.
    /// </summary>
    /// <returns>An observable for observing a collection of client credentials.</returns>
    [HttpGet("observe")]
    public ISubject<IEnumerable<ClientCredentials>> AllClientCredentials() =>
        _clientCredentials.InvokeAndWrapWithTransformSubject(
            token => _clientCredentials.ObserveAll(token),
            clients => clients.ToApi());
}
