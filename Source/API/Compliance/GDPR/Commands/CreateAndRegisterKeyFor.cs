// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Compliance;

namespace Cratis.API.Compliance.GDPR.Commands;

/// <summary>
/// Encapsulation representing the creation and registration of a key for a specific identifier.
/// </summary>
/// <param name="Identifier"><see cref="EncryptionKeyIdentifier"/> the key should be created and registered for.</param>
public record CreateAndRegisterKeyFor(EncryptionKeyIdentifier Identifier);
