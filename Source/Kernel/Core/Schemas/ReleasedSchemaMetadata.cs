// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// A released document and the paths whose protected values were unreadable, independent of placeholder type.
/// </summary>
/// <param name="Value">The released document.</param>
/// <param name="UnreadablePaths">Paths replaced with erasure placeholders.</param>
public record ReleasedSchemaMetadata(JsonObject Value, IReadOnlySet<string> UnreadablePaths);
