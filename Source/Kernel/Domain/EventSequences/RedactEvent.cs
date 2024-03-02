// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Auditing;
using Aksio.Cratis.Events;
using Aksio.Cratis.Identities;

namespace Aksio.Cratis.Kernel.Domain.EventSequences;

/// <summary>
/// Command for redacting single event.
/// </summary>
/// <param name="SequenceNumber">The <see cref="EventSequenceNumber"/> to redact.</param>
/// <param name="Reason">Reason for redacting event.</param>
/// <param name="Causation">Collection of <see cref="Causation"/>.</param>
/// <param name="CausedBy"><see cref="CausedBy"/> to identify the person, system or service that caused the redaction.</param>
public record RedactEvent(
    EventSequenceNumber SequenceNumber,
    RedactionReason Reason,
    IEnumerable<Causation>? Causation,
    Identity? CausedBy);
