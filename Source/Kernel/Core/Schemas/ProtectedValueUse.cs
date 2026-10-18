// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Reports a protected value and the key used. In-memory only; never log or serialize this value.
/// </summary>
/// <param name="Path">The dotted property path with bracketed array indices.</param>
/// <param name="Kind">The protection kind.</param>
/// <param name="Plaintext">A detached copy of the original value.</param>
/// <param name="Key">The encryption key used.</param>
public record ProtectedValueUse(string Path, ProtectionKind Kind, JsonNode Plaintext, EncryptionKeyUse Key);
