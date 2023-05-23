// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Compliance;

namespace Aksio.Cratis.Kernel.Domain.Compliance.GDPR;

/// <summary>
/// Encapsulation representing the creation and registration of a key for a specific identifier.
/// </summary>
/// <param name="Identifier"><see cref="EncryptionKeyIdentifier"/> the key should be created and registered for.</param>
public record CreateAndRegisterKeyFor(EncryptionKeyIdentifier Identifier);
