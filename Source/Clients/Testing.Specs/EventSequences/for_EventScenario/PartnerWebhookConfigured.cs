// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// A partner's webhook was configured to be signed with the namespace's shared secret.
/// </summary>
/// <param name="WebhookUrl">The partner's webhook URL.</param>
/// <param name="Secret">The signing secret - shared by every partner in the namespace.</param>
[EventType]
public record PartnerWebhookConfigured(string WebhookUrl, PartnerWebhookSecret Secret);
