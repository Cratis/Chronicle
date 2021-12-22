// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Compliance.MongoDB
{
    /// <summary>
    /// Represents the stored version of an <see cref="EncryptionKey"/> for a specific <see cref="EncryptionKeyIdentifier"/>.
    /// </summary>
    /// <param name="Identifier"><see cref="EncryptionKeyIdentifier"/> it is for.</param>
    /// <param name="Key">The <see cref="EncryptionKey"/>.</param>
    public record EncryptionKeyForIdentifier(EncryptionKeyIdentifier Identifier, EncryptionKey Key);
}
