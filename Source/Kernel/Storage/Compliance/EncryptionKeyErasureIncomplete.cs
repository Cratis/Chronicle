// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// The exception that is thrown when an <see cref="EncryptionKey"/> was deleted from some, but not all, of the
/// composed key stores.
/// </summary>
/// <remarks>
/// Crypto-shredding only erases a subject when the key is gone from every store that holds it. A key surviving in
/// one store is healed back into the others the next time it is read, so a partial deletion is not an erasure —
/// repeat the deletion once every store is reachable.
/// </remarks>
/// <param name="identifier">The <see cref="EncryptionKeyIdentifier"/> that was being deleted.</param>
/// <param name="failures">The failures reported by the stores the key could not be deleted from.</param>
public class EncryptionKeyErasureIncomplete(EncryptionKeyIdentifier identifier, IReadOnlyList<Exception> failures)
    : Exception(
        $"The encryption key for identifier '{identifier}' could not be deleted from {failures.Count} of the composed key stores. The subject is not erased - a surviving key is healed back into the other stores on the next read, so repeat the deletion once every store is reachable.",
        failures.Count > 0 ? failures[0] : null)
{
    /// <summary>
    /// Gets the failures reported by the stores the key could not be deleted from.
    /// </summary>
    public IReadOnlyList<Exception> Failures { get; } = failures;
}
