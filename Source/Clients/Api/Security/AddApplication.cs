// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents the command for adding application.
/// </summary>
/// <param name="Id">The application identifier.</param>
/// <param name="ClientId">The application's client identifier.</param>
/// <param name="ClientSecret">The application's client secret.</param>
[Command]
public record AddApplication(
    string Id,
    string ClientId,
    string ClientSecret)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="applications">The <see cref="IApplications"/> contract.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(IApplications applications) =>
        applications.Add(new()
        {
            Id = Id,
            ClientId = ClientId,
            ClientSecret = ClientSecret
        });
}
