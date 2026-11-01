// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope;

/// <summary>
/// The type and generation actually delivered.
/// </summary>
/// <param name="Id">The event type identifier.</param>
/// <param name="Generation">The delivered generation.</param>
public record DeliveredEventType(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("generation")] uint Generation);
