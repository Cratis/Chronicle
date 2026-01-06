// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Defines the authentication service for Chronicle.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user with username and password.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="password">The password.</param>
    /// <returns>The authenticated user if successful, null otherwise.</returns>
    Task<ChronicleUser?> AuthenticateUser(string username, string password);

    /// <summary>
    /// Ensures the default admin user exists.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EnsureDefaultAdminUser();

#if DEVELOPMENT && DEBUG
    /// <summary>
    /// Ensures default client credentials exist for development.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EnsureDefaultClientCredentials();
#endif
}
