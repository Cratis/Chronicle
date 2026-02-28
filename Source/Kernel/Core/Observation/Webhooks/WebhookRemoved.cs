// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// Represents the event for a webhook that has been removed.
/// </summary>
[EventType, AllEventStores]
public record WebhookRemoved;
