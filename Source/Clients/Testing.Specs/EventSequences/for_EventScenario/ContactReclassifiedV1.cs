// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// Represents the earlier contact fact, whose alias was private instead of its address.
/// </summary>
/// <param name="Alias">The private alias.</param>
/// <param name="Address">The public address.</param>
[EventTypeGenerationFor<ContactReclassified>(1)]
public record ContactReclassifiedV1([property: PII] string Alias, string Address);
