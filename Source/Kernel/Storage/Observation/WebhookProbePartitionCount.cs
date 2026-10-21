// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// A typed absolute count in a durable probe checkpoint, encoded without concept-valued JSON keys.
/// </summary>
/// <param name="EventTypeId">The counted event type.</param>
/// <param name="Count">The absolute partition count after successful delivery.</param>
public record WebhookProbePartitionCount(EventTypeId EventTypeId, EventCount Count);
