// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security;

/// <summary>
/// Defines tokens used to complete initial administrator setup.
/// </summary>
public interface IInitialAdminSetupTokens
{
    /// <summary>
    /// Issues a token for an administrator.
    /// </summary>
    /// <param name="userId">The administrator identifier.</param>
    /// <returns>The setup token.</returns>
    InitialAdminSetupToken Issue(UserId userId);

    /// <summary>
    /// Validates a setup token for an administrator.
    /// </summary>
    /// <param name="userId">The administrator identifier.</param>
    /// <param name="token">The token to validate.</param>
    /// <returns>Whether the token is valid.</returns>
    bool IsValid(UserId userId, InitialAdminSetupToken? token);
}
