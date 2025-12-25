// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents the command for changing application secret.
/// </summary>
/// <param name="Id">The application identifier.</param>
/// <param name="ClientSecret">The new client secret.</param>
[Command]
public record ChangeApplicationSecret(
    Guid Id,
    string ClientSecret)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="applications">The <see cref="IApplications"/> contract.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(IApplications applications) =>
        applications.ChangeSecret(new()
        {
            Id = Id,
            ClientSecret = ClientSecret
        });
}
