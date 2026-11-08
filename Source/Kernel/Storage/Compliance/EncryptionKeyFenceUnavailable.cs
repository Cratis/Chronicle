// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// A linearizable fence read or verified sweep could not be obtained.
/// </summary>
public class EncryptionKeyFenceUnavailable : Exception
{
    /// <summary>Initializes an unavailable fence for an identifier.</summary>
    /// <param name="identifier">The key identifier.</param>
    public EncryptionKeyFenceUnavailable(EncryptionKeyIdentifier identifier)
        : base($"A linearizable encryption key fence for identifier '{identifier}' is unavailable.")
    {
    }

    /// <summary>Initializes an unavailable fence from a failed read.</summary>
    /// <param name="cause">The failed fence read.</param>
    public EncryptionKeyFenceUnavailable(Exception cause)
        : base("The encryption key erasure fence could not be read; protected material cannot be used.", cause)
    {
    }
}
