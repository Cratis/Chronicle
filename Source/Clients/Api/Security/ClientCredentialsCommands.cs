// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents the API for commands related to client credentials.
/// </summary>
[Route("/api/security/client-credentials")]
public class ClientCredentialsCommands : ControllerBase
{
    readonly IClientCredentials _clientCredentials;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientCredentialsCommands"/> class.
    /// </summary>
    /// <param name="clientCredentials">The <see cref="IClientCredentials"/> contract.</param>
    internal ClientCredentialsCommands(IClientCredentials clientCredentials)
    {
        _clientCredentials = clientCredentials;
    }

    /// <summary>
    /// Add client credentials.
    /// </summary>
    /// <param name="command"><see cref="AddClientCredentials"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("add")]
    public Task Add([FromBody] AddClientCredentials command) =>
        _clientCredentials.Add(new()
        {
            Id = command.Id,
            ClientId = command.ClientId,
            ClientSecret = command.ClientSecret
        });

    /// <summary>
    /// Remove client credentials.
    /// </summary>
    /// <param name="command"><see cref="RemoveClientCredentials"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("remove")]
    public Task Remove([FromBody] RemoveClientCredentials command) =>
        _clientCredentials.Remove(new() { Id = command.Id });

    /// <summary>
    /// Change client credentials secret.
    /// </summary>
    /// <param name="command"><see cref="ChangeClientCredentialsSecret"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("change-secret")]
    public Task ChangeSecret([FromBody] ChangeClientCredentialsSecret command) =>
        _clientCredentials.ChangeSecret(new()
        {
            Id = command.Id,
            ClientSecret = command.ClientSecret
        });
}
