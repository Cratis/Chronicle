// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Api.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Api.Observation.Webhooks;

/// <summary>
/// Represents a command for registering a webhook.
/// </summary>
/// <param name="EventSequenceId">The <see cref="EventSequenceId"/>.</param>
/// <param name="EventTypes">Event types to register.</param>
/// <param name="Target">The <see cref="WebhookTarget"/>.</param>
public record RegisterWebhook(EventSequenceId EventSequenceId, IEnumerable<EventType> EventTypes, WebhookTarget Target);