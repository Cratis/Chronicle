// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable CA1819

namespace Cratis.Compliance
{
    /// <summary>
    /// Represents a key used in encryption.
    /// </summary>
    /// <param name="Public">The public part of the key.</param>
    /// <param name="Private">The private part of the key.</param>
    public record EncryptionKey(byte[] Public, byte[] Private);
}
