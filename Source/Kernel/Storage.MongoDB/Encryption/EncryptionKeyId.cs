// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Compliance.MongoDB;

/// <summary>
/// Represents the composite identifier used as the MongoDB document identity for an encryption key.
/// </summary>
/// <param name="Identifier"><see cref="EncryptionKeyIdentifier"/> of the key.</param>
/// <param name="Revision"><see cref="EncryptionKeyRevision"/> of the key.</param>
public record EncryptionKeyId(EncryptionKeyIdentifier Identifier, EncryptionKeyRevision Revision);
