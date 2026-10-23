// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// An outstanding delivery fence persisted independently of failure episodes.
/// </summary>
/// <param name="Partition">The held partition.</param>
/// <param name="ReleaseBy">The deadline identifying this hold.</param>
/// <param name="Targets">The original delivery targets, or empty for a conservative hold.</param>
public record DeliveryFenceState(Key Partition, DateTimeOffset ReleaseBy, IReadOnlyList<DeliveryFenceTarget> Targets);
