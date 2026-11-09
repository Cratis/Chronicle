// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// The atomic fence guarantees offered by a key store, not its durability.
/// </summary>
[Flags]
public enum EncryptionKeyFenceGuarantees
{
    /// <summary>
    /// No atomic fence guarantees.
    /// </summary>
    None = 0,

    /// <summary>
    /// Fence writes are monotonic and authorization is conditional.
    /// </summary>
    MonotonicFence = 1,

    /// <summary>
    /// Key publication is guarded by a linearizable fence validation.
    /// </summary>
    FencedSave = 2
}
