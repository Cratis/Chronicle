// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>Validates opaque erasure retry identities without normalizing them.</summary>
public static class EncryptionKeyErasureRequestIdentity
{
    /// <summary>Validate a supplied request identity.</summary>
    /// <param name="requestId">The unmodified caller identity.</param>
    /// <exception cref="ArgumentException">The identity is blank or longer than 128 characters.</exception>
    public static void Validate(string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId) || requestId.Length > 128)
        {
            throw new ArgumentException("An erasure request id must contain between 1 and 128 characters and cannot be blank.", nameof(requestId));
        }
    }
}
