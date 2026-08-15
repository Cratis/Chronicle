// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// The exception that is thrown when authorizing a new encryption key for a subject reached some, but not all, of
/// the composed key stores.
/// </summary>
/// <remarks>
/// The stores that were not reached still refuse to provision, so a partial authorization fails closed rather than
/// dangerously - but it leaves the subject able to hold a key in some stores and not others, which shows up later
/// as a forwarded event that cannot be appended. Repeat the authorization once every store is reachable.
/// </remarks>
/// <param name="identifier">The <see cref="EncryptionKeyIdentifier"/> a new key was being authorized for.</param>
/// <param name="failures">The failures reported by the stores that could not be reached.</param>
public class EncryptionKeyLifecycleIncomplete(EncryptionKeyIdentifier identifier, IReadOnlyList<Exception> failures)
    : Exception(
        $"A new encryption key for identifier '{identifier}' could not be authorized in {failures.Count} of the composed key stores. Those stores still refuse to provision one - repeat the authorization once every store is reachable.",
        failures.Count > 0 ? failures[0] : null)
{
    /// <summary>
    /// Gets the failures reported by the stores that could not be reached.
    /// </summary>
    public IReadOnlyList<Exception> Failures { get; } = failures;
}
