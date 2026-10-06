// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;

namespace Cratis.Chronicle.Compliance;

/// <summary>
/// The exception that is thrown when an imported RSA key cannot unwrap a protected value.
/// </summary>
/// <param name="innerException">The RSA decryption failure.</param>
internal class EncryptionKeyUnwrapFailed(CryptographicException innerException)
    : Exception("The encryption key could not unwrap the protected value.", innerException);
