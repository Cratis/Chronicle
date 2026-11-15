// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// The exception that is thrown when an encryption key is present in the key store but disabled.
/// </summary>
/// <remarks>
/// The key is neither used nor replaced. The message deliberately holds no identifier.
/// </remarks>
public class EncryptionKeyUnavailable()
    : Exception("An encryption key is disabled in the key store and cannot be used until it is enabled or erased.");
