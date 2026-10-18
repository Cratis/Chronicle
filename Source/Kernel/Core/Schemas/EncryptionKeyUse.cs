// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Reports the exact key used to protect a value. In-memory only; never log or serialize this value.
/// </summary>
/// <param name="Kind">The protection kind.</param>
/// <param name="Identifier">The scoped key identifier.</param>
/// <param name="Key">The encryption key used.</param>
/// <param name="Revision">The revision read with the key.</param>
public record EncryptionKeyUse(ProtectionKind Kind, EncryptionKeyIdentifier Identifier, EncryptionKey Key, EncryptionKeyRevision Revision)
{
    /// <summary>
    /// Gets the key's non-secret reference.
    /// </summary>
    public string KeyReference => Key.Fingerprint;
}
