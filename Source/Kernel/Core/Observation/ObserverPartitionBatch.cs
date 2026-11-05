// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the events of one partition within a page that spans partitions.
/// </summary>
/// <param name="Partition">The <see cref="Key"/> of the partition.</param>
/// <param name="Events">The events of the partition, in sequence order.</param>
public record ObserverPartitionBatch(Key Partition, IReadOnlyList<AppendedEvent> Events);
