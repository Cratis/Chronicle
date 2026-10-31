// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// Represents a key and its revision obtained from the same stored entry.
/// </summary>
/// <param name="Key">The encryption key.</param>
/// <param name="Revision">The revision holding the key.</param>
public record EncryptionKeyAtRevision(EncryptionKey Key, EncryptionKeyRevision Revision);
