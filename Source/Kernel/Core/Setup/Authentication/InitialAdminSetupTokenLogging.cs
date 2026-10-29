// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using User = Cratis.Chronicle.Storage.Security.User;

namespace Cratis.Chronicle.Setup.Authentication;

/// <summary>
/// Logs the operator token while initial administrator setup is pending.
/// </summary>
/// <param name="tokens">The setup token provider.</param>
/// <param name="options">The Chronicle options.</param>
/// <param name="logger">The authentication service logger.</param>
internal class InitialAdminSetupTokenLogging(IInitialAdminSetupTokens tokens, IOptions<ChronicleOptions> options, ILogger<AuthenticationService> logger)
{
    /// <summary>
    /// Logs the setup token when the administrator still requires interactive setup.
    /// </summary>
    /// <param name="user">The persisted administrator.</param>
    /// <param name="bootstrapPassword">The configured bootstrap password, if any.</param>
    internal void LogIfRequired(User? user, string bootstrapPassword)
    {
        if (user?.HasLoggedIn is not false)
        {
            return;
        }
        if (!string.IsNullOrEmpty(user.PasswordHash?.Value))
        {
            return;
        }
        if (!string.IsNullOrEmpty(bootstrapPassword))
        {
            return;
        }

        var configured = options.Value.Authentication.InitialAdminSetupToken;
        var token = string.IsNullOrWhiteSpace(configured) ? tokens.Issue(user.Id).Value : configured;
        logger.InitialAdministratorSetupToken(token);
    }
}
