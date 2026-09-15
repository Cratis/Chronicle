// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

/// <summary>
/// Records a contact whose address is private in the current generation.
/// </summary>
/// <param name="Alias">The public alias.</param>
/// <param name="Address">The private address.</param>
[EventType(generation: 2)]
public record ContactReclassified(string Alias, [property: PII] string Address);
