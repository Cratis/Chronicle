// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Sequencing;

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Represents an S row identified by observer, generation, key hash and start position.
/// </summary>
/// <param name="Observer">The observer.</param>
/// <param name="Generation">The row generation.</param>
/// <param name="Key">The consistent key identity.</param>
/// <param name="From">The inclusive start.</param>
/// <param name="To">The exclusive end.</param>
/// <param name="Reason">The skip reason.</param>
/// <param name="Recorded">The recording time.</param>
/// <param name="Revision">The committing root revision.</param>
public sealed record SkipRange(
    ObserverId Observer,
    DeliveryGeneration Generation,
    SequencingKeyIdentity Key,
    EventSequenceNumber From,
    EventSequenceNumber To,
    SkipReason Reason,
    DateTimeOffset Recorded,
    DeliveryRevision Revision);
