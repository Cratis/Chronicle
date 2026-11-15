// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Represents a released value and its protection status.
/// </summary>
/// <param name="Value">The released value, or the original ciphertext when erased.</param>
/// <param name="Status">The release outcome.</param>
public record ReleasedProtectedValue(JsonNode Value, ProtectedValueReleaseStatus Status);
