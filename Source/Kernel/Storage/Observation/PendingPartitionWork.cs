// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Represents pending work for a partition in the shadow ownership protocol.
/// </summary>
/// <param name="Partition">The partition awaiting work.</param>
/// <param name="Holder">The kind of work requested.</param>
/// <param name="From">The position from which work is requested.</param>
/// <param name="RequestedAt">When the work was requested.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Storage.Observation.PendingPartitionWork")]
public record PendingPartitionWork([Id(0)] Key Partition, [Id(1)] ClaimHolderKind Holder, [Id(2)] EventSequenceNumber From, [Id(3)] DateTimeOffset RequestedAt);
