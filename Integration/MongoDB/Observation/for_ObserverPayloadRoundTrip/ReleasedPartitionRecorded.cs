// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.MongoDB.Integration.Observation.for_ObserverPayloadRoundTrip;

/// <summary>
/// An event consumed only by the released-kernel failed-partition probe.
/// </summary>
/// <param name="Value">The probe's payload.</param>
[EventType("released-partition-recorded")]
public record ReleasedPartitionRecorded(string Value);
