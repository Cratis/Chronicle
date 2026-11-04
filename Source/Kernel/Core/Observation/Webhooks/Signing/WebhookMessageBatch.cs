// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Represents the range of one partition's events that a webhook message carries.
/// </summary>
/// <param name="Partition">The <see cref="Key">partition</see> the events belong to.</param>
/// <param name="First">The first <see cref="EventSequenceNumber"/> in the batch.</param>
/// <param name="Last">The last <see cref="EventSequenceNumber"/> in the batch.</param>
public record WebhookMessageBatch(Key Partition, EventSequenceNumber First, EventSequenceNumber Last);
