// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope;

/// <summary>
/// A single partition's events.
/// </summary>
/// <param name="Partition">The partition key.</param>
/// <param name="Events">The events in sequence order.</param>
/// <param name="FencingToken">Reserved partition claim epoch; null in v1.</param>
/// <param name="InitialState">Optional reducer state.</param>
public record DeliveryBatch(
    [property: JsonPropertyName("partition")] string Partition,
    [property: JsonPropertyName("events")] IReadOnlyList<DeliveredEvent> Events,
    [property: JsonPropertyName("fencingToken"), JsonIgnore(Condition = JsonIgnoreCondition.Never)] ulong? FencingToken = null,
    [property: JsonPropertyName("initialState"), JsonIgnore(Condition = JsonIgnoreCondition.Never)] JsonNode? InitialState = null);
