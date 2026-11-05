// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// The exception that is thrown when an encryption key is stored under an earlier name that cannot be addressed unambiguously.
/// </summary>
/// <remarks>
/// The key is neither used nor replaced. Run the key upgrade to move it. The message deliberately holds no identifier.
/// </remarks>
public class EncryptionKeyNeedsUpgrade()
    : Exception("An encryption key stored under an earlier name needs the key upgrade before it can be used.");
