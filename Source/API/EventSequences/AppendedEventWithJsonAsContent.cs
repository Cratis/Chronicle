// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.API.EventSequences;

/// <summary>
/// Represents an event that has been appended to an event log with the content as JSON.
/// </summary>
/// <param name="Metadata">The <see cref="EventMetadata"/>.</param>
/// <param name="Context">The <see cref="EventContext"/>.</param>
/// <param name="Content">The content in the form of an <see cref="JsonObject"/>.</param>
public record AppendedEventWithJsonAsContent(EventMetadata Metadata, EventContext Context, JsonNode Content);
