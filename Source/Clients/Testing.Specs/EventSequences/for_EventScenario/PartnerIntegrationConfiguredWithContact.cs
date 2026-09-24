// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// A partner integration was configured, carrying both a [PII] contact email and an [Encrypted] signing secret
/// for the same event source - proving the two protections coexist cleanly on one event.
/// </summary>
/// <param name="ContactEmail">The partner contact's email address.</param>
/// <param name="ApiKey">The partner's API key.</param>
[EventType]
public record PartnerIntegrationConfiguredWithContact(MemberEmailAddress ContactEmail, PartnerApiKey ApiKey);
