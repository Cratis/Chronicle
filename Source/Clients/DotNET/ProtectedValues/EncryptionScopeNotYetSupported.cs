// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>
/// The exception that is thrown when an <see cref="EncryptedAttribute"/> requests an <see cref="EncryptionScope"/>
/// the kernel does not yet honor.
/// </summary>
/// <remarks>
/// Every current <see cref="EncryptionScope"/> member (<see cref="EncryptionScope.Subject"/>, <see cref="EncryptionScope.Namespace"/>,
/// <see cref="EncryptionScope.Global"/>) is honored. This is the backstop for a future scope added to the enum before
/// the kernel is taught to provision a key identity for it - thrown at metadata-provision time (schema generation,
/// which normally happens once at startup when event types are registered) rather than left to fail silently or
/// fall back to a scope that was not asked for.
/// </remarks>
/// <param name="scope">The unsupported <see cref="EncryptionScope"/>.</param>
public class EncryptionScopeNotYetSupported(EncryptionScope scope)
    : Exception($"EncryptionScope.{scope} is not supported by [Encrypted]. This scope value has no corresponding kernel handler yet.");
