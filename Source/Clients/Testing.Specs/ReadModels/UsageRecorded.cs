// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event recording consumed capacity against a named period, used to verify accumulating
/// projections both at the root and inside a child collection.
/// </summary>
/// <param name="Period">The period the usage belongs to, used as the child key.</param>
/// <param name="Consumed">The amount consumed.</param>
[EventType]
public record UsageRecorded(string Period, decimal Consumed);
