// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Tracks failed password sign-ins for a username and remote address.
/// </summary>
public interface ISignInThrottle
{
    /// <summary>
    /// Checks whether further password checks must be deferred.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="address">The remote address.</param>
    /// <returns>Whether the pair is locked out.</returns>
    bool IsLockedOut(string username, string address);

    /// <summary>
    /// Records an unsuccessful password check.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="address">The remote address.</param>
    void Failed(string username, string address);

    /// <summary>
    /// Clears failures after a successful password check.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="address">The remote address.</param>
    void Succeeded(string username, string address);
}
