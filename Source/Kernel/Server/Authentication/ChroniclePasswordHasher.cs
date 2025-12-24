// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Security;
using Cratis.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;

namespace Cratis.Chronicle.Server.Authentication;

/// <summary>
/// Custom password hasher for Chronicle that uses the HashHelper.
/// </summary>
public class ChroniclePasswordHasher : IPasswordHasher<ChronicleUser>
{
    /// <inheritdoc/>
    public string HashPassword(ChronicleUser user, string password)
    {
        return HashHelper.Hash(password);
    }

    /// <inheritdoc/>
    public PasswordVerificationResult VerifyHashedPassword(ChronicleUser user, string hashedPassword, string providedPassword)
    {
        var isValid = HashHelper.Verify(providedPassword, hashedPassword);
        return isValid ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
    }
}
