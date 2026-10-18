// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Locates a protected value without accessing its key. In-memory only; never log or serialize this value.
/// </summary>
/// <param name="Path">The dotted property path with bracketed array indices.</param>
/// <param name="Kind">The protection kind.</param>
/// <param name="Identifier">The scoped key identifier.</param>
/// <param name="Plaintext">A detached copy of the original value.</param>
public record ProtectedValueLocation(string Path, ProtectionKind Kind, EncryptionKeyIdentifier Identifier, JsonNode Plaintext);
