// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents the API for commands related to application.
/// </summary>
[Route("/api/security/client-credentials")]
public class ApplicationCommands : ControllerBase
{
    readonly IApplications _applications;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApplicationCommands"/> class.
    /// </summary>
    /// <param name="applications">The <see cref="IApplications"/> contract.</param>
    internal ApplicationCommands(IApplications applications)
    {
        _applications = applications;
    }

    /// <summary>
    /// Add application.
    /// </summary>
    /// <param name="command"><see cref="AddApplication"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("add")]
    public Task Add([FromBody] AddApplication command) =>
        _applications.Add(new()
        {
            Id = command.Id,
            ClientId = command.ClientId,
            ClientSecret = command.ClientSecret
        });

    /// <summary>
    /// Remove application.
    /// </summary>
    /// <param name="command"><see cref="RemoveApplication"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("remove")]
    public Task Remove([FromBody] RemoveApplication command) =>
        _applications.Remove(new() { Id = command.Id });

    /// <summary>
    /// Change application secret.
    /// </summary>
    /// <param name="command"><see cref="ChangeApplicationSecret"/> command.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("change-secret")]
    public Task ChangeSecret([FromBody] ChangeApplicationSecret command) =>
        _applications.ChangeSecret(new()
        {
            Id = command.Id,
            ClientSecret = command.ClientSecret
        });
}
