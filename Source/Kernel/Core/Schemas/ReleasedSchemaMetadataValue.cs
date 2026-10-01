// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// A released value and whether its protected contents could not be read.
/// </summary>
/// <param name="Value">The decrypted or pass-through value.</param>
/// <param name="IsUnreadable">Whether the value needs an erasure placeholder rather than plaintext shape restoration.</param>
public record ReleasedSchemaMetadataValue(JsonNode Value, bool IsUnreadable = false);
