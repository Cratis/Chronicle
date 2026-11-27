// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// A durably accepted repair covering the bounded interval from <paramref name="From"/> to <paramref name="To"/>.
/// Observer-wide delivery and successors wait until every accepted repair acknowledges completion.
/// </summary>
/// <param name="Partition">The partition needing repair.</param>
/// <param name="From">The first potentially missed event, inclusive.</param>
/// <param name="To">The accepted cursor, exclusive of the original missed interval.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Storage.Observation.PendingCatchUpRepair")]
public record PendingCatchUpRepair([Id(0)] Key Partition, [Id(1)] EventSequenceNumber From, [Id(2)] EventSequenceNumber To);
