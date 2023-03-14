// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events;

/// <summary>
/// Command for redacting single event.
/// </summary>
/// <param name="SequenceNumber">The <see cref="EventSequenceNumber"/> to redact.</param>
/// <param name="Reason">Reason for redacting event.</param>
public record RedactEvent(EventSequenceNumber SequenceNumber, RedactionReason Reason);
