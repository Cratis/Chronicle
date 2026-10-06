// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Jobs;

/// <summary>
/// Represents a partition an observer-wide catch-up left behind, and where reading it has to resume.
/// </summary>
/// <param name="Partition">The partition, as a <see cref="Key"/>.</param>
/// <param name="FromEventSequenceNumber">The <see cref="EventSequenceNumber"/> its catch-up step had read up to.</param>
public record CatchUpObserverPartitionRange(Key Partition, EventSequenceNumber FromEventSequenceNumber);
