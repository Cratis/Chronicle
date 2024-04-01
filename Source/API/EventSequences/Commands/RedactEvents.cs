// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Kernel.Contracts.Auditing;
using Cratis.Kernel.Contracts.Identities;

namespace Cratis.API.EventSequences.Commands;

/// <summary>
/// Command for redacting events.
/// </summary>
/// <param name="EventSourceId">The <see cref="EventSourceId"/> to redact.</param>
/// <param name="Reason">Reason for redacting event.</param>
/// <param name="EventTypes">Any specific event types to redact. Can be empty.</param>
/// <param name="Causation">Collection of <see cref="Causation"/>.</param>
/// <param name="CausedBy"><see cref="CausedBy"/> to identify the person, system or service that caused the redaction.</param>
public record RedactEvents(
    string EventSourceId,
    string Reason,
    IEnumerable<string> EventTypes,
    IEnumerable<Causation>? Causation,
    Identity? CausedBy);
