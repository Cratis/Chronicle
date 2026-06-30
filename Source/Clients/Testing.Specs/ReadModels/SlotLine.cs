// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A child line keyed by a <see cref="TimeOnly"/> slot.
/// </summary>
/// <param name="Slot">The time-of-day slot, used as the child key.</param>
/// <param name="Amount">The recorded amount.</param>
public record SlotLine(
    TimeOnly Slot,
    decimal Amount);
