// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Security;

namespace Cratis.Chronicle.Api.Security;

/// <summary>
/// Represents the command for removing application.
/// </summary>
/// <param name="Id">The application identifier.</param>
[Command]
public record RemoveApplication(string Id)
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="applications">The <see cref="IApplications"/> contract.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(IApplications applications) =>
        applications.Remove(new()
        {
            Id = Id,
        });
}
