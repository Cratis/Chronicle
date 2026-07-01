// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event recording an amount against a day on a ledger, where the day (the child key) is a
/// <see cref="DateOnly"/> and the event lives on its own event source, linked to the parent via
/// <paramref name="LedgerId"/>.
/// </summary>
/// <param name="LedgerId">The parent ledger identifier (used as the parent key).</param>
/// <param name="Day">The business day, used as the child key.</param>
/// <param name="Amount">The recorded amount.</param>
[EventType]
public record DayRaised(Guid LedgerId, DateOnly Day, decimal Amount);
