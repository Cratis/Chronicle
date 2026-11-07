// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// A linearizable fence read or verified sweep could not be obtained.
/// </summary>
/// <param name="identifier">The key identifier.</param>
public class EncryptionKeyFenceUnavailable(EncryptionKeyIdentifier identifier)
    : Exception($"A linearizable encryption key fence for identifier '{identifier}' is unavailable.");
