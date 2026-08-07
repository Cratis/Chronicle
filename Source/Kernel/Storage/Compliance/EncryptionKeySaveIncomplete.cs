// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// The exception that is thrown when an <see cref="EncryptionKey"/> was saved to some, but not all, of the
/// composed key stores.
/// </summary>
/// <remarks>
/// Every store is attempted before this is raised, so the reachable stores do hold the key. The stores are no
/// longer in step though, which matters while a cutover is in progress: a rollback to a store that missed the
/// write cannot read back the values protected by it.
/// </remarks>
/// <param name="identifier">The <see cref="EncryptionKeyIdentifier"/> that was being saved.</param>
/// <param name="failures">The failures reported by the stores the key could not be saved to.</param>
public class EncryptionKeySaveIncomplete(EncryptionKeyIdentifier identifier, IReadOnlyList<Exception> failures)
    : Exception(
        $"The encryption key for identifier '{identifier}' could not be saved to {failures.Count} of the composed key stores, so the stores no longer hold the same keys.",
        failures.Count > 0 ? failures[0] : null)
{
    /// <summary>
    /// Gets the failures reported by the stores the key could not be saved to.
    /// </summary>
    public IReadOnlyList<Exception> Failures { get; } = failures;
}
