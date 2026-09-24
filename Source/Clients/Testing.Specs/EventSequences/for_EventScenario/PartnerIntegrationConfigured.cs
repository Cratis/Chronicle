// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// A partner integration was configured with a signing secret.
/// </summary>
/// <param name="ApiKey">The partner's API key.</param>
[EventType]
public record PartnerIntegrationConfigured(PartnerApiKey ApiKey);
