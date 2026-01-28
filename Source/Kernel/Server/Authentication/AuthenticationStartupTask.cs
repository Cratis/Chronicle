// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Represents an <see cref="IStartupTask"/> for initializing authentication defaults.
/// </summary>
/// <param name="authenticationService">The <see cref="IAuthenticationService"/>.</param>
public class AuthenticationStartupTask(IAuthenticationService authenticationService) : IStartupTask
{
    /// <inheritdoc/>
    public async Task Execute(CancellationToken cancellationToken)
    {
        await authenticationService.EnsureDefaultAdminUser();
#if DEVELOPMENT
        await authenticationService.EnsureDefaultClientCredentials();
#endif
    }
}
