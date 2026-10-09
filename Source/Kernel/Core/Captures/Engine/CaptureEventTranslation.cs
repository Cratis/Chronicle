// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Captures.Engine;

/// <summary>
/// Represents the result of translating one incoming event.
/// </summary>
/// <param name="Key">The key the event resolved to, which is also the event source of the appended events.</param>
/// <param name="Current">The state to remember for the key - what the key's events have said so far, overlaid with the incoming event content.</param>
/// <param name="Events">The private events to append.</param>
public record CaptureEventTranslation(string Key, JsonObject Current, IReadOnlyList<TranslatedCaptureEvent> Events);
