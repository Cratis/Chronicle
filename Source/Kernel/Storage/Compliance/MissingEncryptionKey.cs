// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Exception that gets thrown when an <see cref="EncryptionKey"/> is missing.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingEncryptionKey"/> class.
/// </remarks>
/// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> that is missing.</param>
public class MissingEncryptionKey(EncryptionKeyIdentifier identifier) : Exception($"Missing encryption key for identifier '{identifier}'")
{
}
