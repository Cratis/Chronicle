// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// The observed fence no longer permits a conditional operation.
/// </summary>
/// <param name="identifier">The key identifier.</param>
public class EncryptionKeyFenceChanged(EncryptionKeyIdentifier identifier)
    : Exception($"The encryption key fence for identifier '{identifier}' changed. Read the fence again before authorizing a new lifecycle.");
