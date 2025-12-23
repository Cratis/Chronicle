// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Provides hashing helpers for secrets.
/// </summary>
public static class HashHelper
{
    /// <summary>
    /// Hash a value using PBKDF2 with a random salt.
    /// </summary>
    /// <param name="value">The value to hash.</param>
    /// <returns>The salted and hashed value.</returns>
    public static string Hash(string value)
    {
        var salt = RandomNumberGenerator.GetBytes(128 / 8);
        var hashed = KeyDerivation.Pbkdf2(
            password: value,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8);

        return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hashed);
    }

    /// <summary>
    /// Verify a value against a salted PBKDF2 hash.
    /// </summary>
    /// <param name="value">The value to verify.</param>
    /// <param name="hashedValue">The stored salted hash.</param>
    /// <returns>True if the hash matches, otherwise false.</returns>
    public static bool Verify(string value, string hashedValue)
    {
        var parts = hashedValue.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[0]);
        var hash = Convert.FromBase64String(parts[1]);

        var testHash = KeyDerivation.Pbkdf2(
            password: value,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8);

        return hash.SequenceEqual(testHash);
    }
}
