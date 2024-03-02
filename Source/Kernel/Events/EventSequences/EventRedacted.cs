// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;

namespace Aksio.Cratis.Kernel.Events.EventSequences;

/// <summary>
/// The event that occurs when an event is redacted.
/// </summary>
/// <param name="Microservice">The <see cref="MicroserviceId"/> the event was redacted for.</param>
/// <param name="TenantId">The <see cref="Execution.TenantId"/> the event was redacted for.</param>
/// <param name="Sequence"><see cref="EventSequenceId"/> the event was in.</param>
/// <param name="SequenceNumber"><see cref="EventSequenceNumber"/> of the event that was redacted.</param>
/// <param name="Reason"><see cref="RedactionReason"/> representing why it was redacted.</param>
[EventType("81de07b7-6aad-40cd-a8e1-028ecb33fda6")]
public record EventRedacted(
    MicroserviceId Microservice,
    TenantId TenantId,
    EventSequenceId Sequence,
    EventSequenceNumber SequenceNumber,
    RedactionReason Reason);
