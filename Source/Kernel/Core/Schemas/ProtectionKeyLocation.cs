// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Describes a protection key without reading or provisioning it.
/// </summary>
/// <param name="Kind">The protection kind.</param>
/// <param name="Identifier">The scoped key identifier.</param>
public record ProtectionKeyLocation(ProtectionKind Kind, EncryptionKeyIdentifier Identifier);
