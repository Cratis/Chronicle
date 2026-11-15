// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Compliance;

/// <summary>
/// The key-lifecycle operations that fan out across event stores.
/// </summary>
public enum ComplianceKeyOperation
{
    /// <summary>
    /// Delete the encryption key. A store is affected when it holds a key for the identifier.
    /// </summary>
    Delete = 0,

    /// <summary>
    /// Allow a new encryption key. A store is affected when it holds an erasure for the identifier.
    /// </summary>
    AllowNew = 1
}
