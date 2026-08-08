// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// The exception that is thrown when no composed key store produced an <see cref="EncryptionKey"/> and at least
/// one of them could not be reached, so the absence of a key cannot be trusted.
/// </summary>
/// <remarks>
/// Reporting this as "no key" would be indistinguishable from a completed right-to-erasure: every value protected
/// under the key would read back as an empty string, with nothing anywhere saying why. Failing loudly is the
/// safe answer — restore the unreachable store and the values reappear.
/// </remarks>
/// <param name="identifier">The <see cref="EncryptionKeyIdentifier"/> that was being read.</param>
/// <param name="failures">The failures reported by the stores that could not be reached.</param>
public class EncryptionKeyStorageUnavailable(EncryptionKeyIdentifier identifier, IReadOnlyList<Exception> failures)
    : Exception(
        $"No encryption key was found for identifier '{identifier}' and {failures.Count} of the composed key stores could not be reached, so the key cannot be reported as absent.",
        failures.Count > 0 ? failures[0] : null)
{
    /// <summary>
    /// Gets the failures reported by the stores that could not be reached.
    /// </summary>
    public IReadOnlyList<Exception> Failures { get; } = failures;
}
