// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>
/// The exception that is thrown when an <see cref="EncryptedAttribute"/> requests an <see cref="EncryptionScope"/>
/// the kernel does not yet honor.
/// </summary>
/// <remarks>
/// Thrown at metadata-provision time - schema generation, which normally happens once at startup when event
/// types are registered - rather than left to fail silently or fall back to a scope that was not asked for.
/// <see cref="EncryptionScope.Namespace"/> and <see cref="EncryptionScope.Global"/> are reserved for a
/// deliberately separate piece of work: they need a key identity that does not depend on a document carrying a
/// subject at all, which <see cref="EncryptionScope.Subject"/> does not need.
/// </remarks>
/// <param name="scope">The unsupported <see cref="EncryptionScope"/>.</param>
public class EncryptionScopeNotYetSupported(EncryptionScope scope)
    : Exception($"EncryptionScope.{scope} is not yet supported by [Encrypted]. Only EncryptionScope.Subject is implemented today; namespace- and installation-scoped keys are tracked as a separate, deliberately unbundled piece of work.");
