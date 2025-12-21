// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Infrastructure.Security;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Provides hashing helpers for secrets.
/// </summary>
[Obsolete("Use Cratis.Infrastructure.Security.HashHelper instead")]
public static class HashHelper
{
    /// <summary>
    /// Hash a value using PBKDF2 with a random salt.
    /// </summary>
    /// <param name="value">The value to hash.</param>
    /// <returns>The salted and hashed value.</returns>
    public static string Hash(string value) => Cratis.Infrastructure.Security.HashHelper.Hash(value);

    /// <summary>
    /// Verify a value against a salted PBKDF2 hash.
    /// </summary>
    /// <param name="value">The value to verify.</param>
    /// <param name="hashedValue">The stored salted hash.</param>
    /// <returns>True if the hash matches, otherwise false.</returns>
    public static bool Verify(string value, string hashedValue) => Cratis.Infrastructure.Security.HashHelper.Verify(value, hashedValue);
}
