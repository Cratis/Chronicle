// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks;

/// <summary>
/// The exception that is thrown when an event that records a webhook registration could not be appended.
/// </summary>
/// <param name="webhookId">The identifier of the webhook being registered.</param>
/// <param name="eventType">The type of the event that could not be appended.</param>
/// <remarks>
/// The message names the webhook and the event type only. The event itself can carry webhook secrets, so neither its
/// content nor the append errors describing it are part of the message.
/// </remarks>
public class WebhookRegistrationFailed(string webhookId, Type eventType)
    : Exception($"Registering webhook '{webhookId}' failed, the '{eventType.Name}' event could not be appended");
