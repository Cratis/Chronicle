// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Kernel.Contracts.Events;

namespace Cratis.API.EventTypes.Queries;

/// <summary>
/// Represents an event type with all its schemas.
/// </summary>
/// <param name="Type">Event type.</param>
/// <param name="Schemas">Collection of schemas.</param>
public record EventTypeWithSchemas(EventType Type, IEnumerable<JsonDocument> Schemas);
