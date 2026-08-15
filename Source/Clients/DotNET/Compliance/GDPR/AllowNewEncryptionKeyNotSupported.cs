// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR;

/// <summary>
/// The exception that is thrown when an <see cref="IPIIManager"/> implementation does not support authorizing a new
/// encryption key for a subject whose key was erased.
/// </summary>
/// <remarks>
/// <see cref="IPIIManager.AllowNewEncryptionKeyFor(EncryptionKeyIdentifier)"/> carries a default implementation that
/// throws this, so an implementation written before the member existed keeps compiling rather than breaking on
/// upgrade. Authorizing a new key cannot be given a meaningful default - it is the deliberate act that lets a subject
/// be protected again after erasure - so an implementation that means to support it has to say so by overriding the
/// member.
/// </remarks>
/// <param name="implementationType">The <see cref="Type"/> of the implementation that does not support it.</param>
public class AllowNewEncryptionKeyNotSupported(Type implementationType)
    : Exception($"'{implementationType.FullName}' does not implement IPIIManager.AllowNewEncryptionKeyFor. Override it to support authorizing a new encryption key for a subject whose key was erased.");
