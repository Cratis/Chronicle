// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Webhooks;

namespace Cratis.Chronicle.Observation.Webhooks.Signing;

/// <summary>
/// Represents the event for a webhook signing key that has been superseded by a newer key.
/// </summary>
/// <param name="KeyId">The identifier of the superseded key.</param>
/// <param name="OverlapEndsAt">When the superseded key stops being valid.</param>
[EventType, AllEventStores]
public record WebhookSigningKeySuperseded(WebhookSigningKeyId KeyId, DateTimeOffset OverlapEndsAt);
