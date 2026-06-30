// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event recording an amount against a time-of-day slot, keyed by a <see cref="TimeOnly"/>.
/// </summary>
/// <param name="Slot">The time-of-day slot, used as the child key.</param>
/// <param name="Amount">The recorded amount.</param>
[EventType]
public record SlotAmountRecorded(TimeOnly Slot, decimal Amount);
