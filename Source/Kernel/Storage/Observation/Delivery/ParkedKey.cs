// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Sequencing;

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Represents an F row identified by observer, generation and key hash.
/// </summary>
/// <param name="Observer">The observer.</param>
/// <param name="Generation">The row generation.</param>
/// <param name="Key">The consistent key identity.</param>
/// <param name="Next">The next position for this key.</param>
/// <param name="Kind">The reason the key is parked.</param>
/// <param name="Attempts">The non-negative attempt count.</param>
/// <param name="RetryDue">The next retry time in UTC ticks.</param>
/// <param name="ReadyTicket">The ready ordering ticket.</param>
/// <param name="Quarantined">Whether the key is quarantined.</param>
/// <param name="Revision">The committing root revision.</param>
/// <param name="Failure">Optional failure diagnostics.</param>
public sealed record ParkedKey(
    ObserverId Observer,
    DeliveryGeneration Generation,
    SequencingKeyIdentity Key,
    EventSequenceNumber Next,
    ParkedKeyKind Kind,
    int Attempts,
    long RetryDue,
    DeliveryTicket ReadyTicket,
    bool Quarantined,
    DeliveryRevision Revision,
    ParkedKeyFailure? Failure);
