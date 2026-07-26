// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.for_AccumulatorRedeliverySafety;

/// <summary>
/// The event the accumulators fold. One event equals one unit, so the expected final state of every accumulator is
/// exactly the number of appended events.
/// </summary>
/// <param name="Amount">The amount the event contributes.</param>
[EventType]
public record AmountRecorded(int Amount);
