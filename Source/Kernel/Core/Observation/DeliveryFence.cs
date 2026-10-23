// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents a versioned fence for one partition and its original delivery targets.
/// </summary>
/// <param name="Partition">The partition.</param>
/// <param name="ReleaseBy">The release deadline, also used to reject stale releases.</param>
/// <param name="Targets">The captured original targets; empty means hold conservatively.</param>
public record DeliveryFence(Key Partition, DateTimeOffset ReleaseBy, IReadOnlyList<DeliveryFenceTarget> Targets);
