// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.API.Auditing;
using Cratis.API.Identities;

namespace Cratis.API.EventSequences.Commands;

/// <summary>
/// Command for redacting single event.
/// </summary>
/// <param name="SequenceNumber">The event sequence number to redact.</param>
/// <param name="Reason">Reason for redacting event.</param>
/// <param name="Causation">Collection of <see cref="Causation"/>.</param>
/// <param name="CausedBy"><see cref="CausedBy"/> to identify the person, system or service that caused the redaction.</param>
public record RedactEvent(
    ulong SequenceNumber,
    string Reason,
    IEnumerable<Causation>? Causation,
    Identity? CausedBy);
