// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Represents a change observed for a single item between two capture cycles.
/// </summary>
/// <param name="Key">The value of the capture's key property identifying the item.</param>
/// <param name="Type">The <see cref="CaptureChangeType"/>.</param>
/// <param name="Previous">The item as it was previously observed - null when the item was added.</param>
/// <param name="Current">The item as it is currently observed - null when the item was removed.</param>
public record CaptureChange(string Key, CaptureChangeType Type, JsonObject? Previous, JsonObject? Current);
