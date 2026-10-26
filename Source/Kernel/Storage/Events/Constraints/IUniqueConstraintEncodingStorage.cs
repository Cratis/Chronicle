// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Events.Constraints;

/// <summary>
/// Defines event-store-level storage for the stable unique-constraint encoding key, shared by all namespaces.
/// </summary>
/// <remarks>
/// Reads never create keys. Absence is not permission to generate a replacement: losing the key makes existing
/// v2 values unverifiable. No live caller may provision on first use. Migration, rotation, and restoration
/// require a separate offline process; this contract cannot replace a key.
/// Future marker admission must validate marker/key identity and forbid creation when a marker exists but
/// the key is missing, within the same lock or transaction as creation, never through a separate read check.
/// </remarks>
public interface IUniqueConstraintEncodingStorage
{
    /// <summary>
    /// Reads the stored identity and material without modifying storage or generating a key.
    /// </summary>
    /// <returns>The stored key, or null when none has been provisioned.</returns>
    Task<UniqueConstraintEncodingKey?> GetKey();

    /// <summary>
    /// Atomically inserts the supplied key only if no key exists, returning the winning pair in either case.
    /// </summary>
    /// <param name="candidate">The identity and material to insert together.</param>
    /// <returns>The newly stored key or the existing key. A losing candidate is never persisted.</returns>
    Task<UniqueConstraintEncodingKey> CreateKeyIfAbsent(UniqueConstraintEncodingKey candidate);
}
