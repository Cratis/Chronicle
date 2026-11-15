// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Compliance;

/// <summary>
/// The exception that is thrown when an <see cref="IEncryptionKeyStorage"/> cannot hold kernel-internal secrets.
/// </summary>
/// <remarks>
/// An internal secret has to be kept apart from subject keys, so a store that cannot do so refuses rather than
/// storing it among them. Features that depend on the secret fail closed until a store that supports internal secrets
/// is configured.
/// </remarks>
/// <param name="purpose">The <see cref="InternalSecretPurpose"/> that was requested.</param>
public class InternalSecretsNotSupported(InternalSecretPurpose purpose)
    : Exception($"The configured encryption key store cannot hold the internal secret '{purpose}' apart from subject keys. Configure a key store that supports internal secrets.");
