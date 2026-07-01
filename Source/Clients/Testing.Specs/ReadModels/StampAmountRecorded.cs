// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event recording an amount against a timestamp, keyed by a <see cref="DateTime"/>.
/// </summary>
/// <param name="Stamp">The timestamp, used as the child key.</param>
/// <param name="Amount">The recorded amount.</param>
[EventType]
public record StampAmountRecorded(DateTime Stamp, decimal Amount);
