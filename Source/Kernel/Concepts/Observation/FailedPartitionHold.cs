// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Concepts.Observation;

/// <summary>
/// Records why a partition is parked, without adding a failure attempt or discarding its recovery cursor.
/// </summary>
/// <param name="Reason">The reason delivery is held.</param>
/// <param name="AwaitedEventType">The event-type identifier for which no connected handler is available.</param>
/// <param name="Since">When the hold began, for reporting its age.</param>
public record FailedPartitionHold(FailedPartitionHoldReason Reason, EventTypeId AwaitedEventType, DateTimeOffset Since);
