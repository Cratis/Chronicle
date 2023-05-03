// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Aksio.Cratis.Kernel.Read.EventTypes;

/// <summary>
/// Represents an event type with all its schemas.
/// </summary>
/// <param name="EventType">Event type.</param>
/// <param name="Schemas">Collection of schemas.</param>
public record EventTypeWithSchemas(EventTypeInformation EventType, IEnumerable<JsonDocument> Schemas);
