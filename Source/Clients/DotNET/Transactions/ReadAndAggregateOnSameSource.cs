// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when an aggregate and protected read guard the same source.
/// </summary>
/// <param name="label">The shared source.</param>
/// <param name="read">The protected read.</param>
/// <param name="aggregate">The aggregate.</param>
public class ReadAndAggregateOnSameSource(EventSourceId label, string read, string aggregate)
    : Exception($"The command reads '{read}' and aggregate '{aggregate}' on source '{label}'. Read protection cannot combine these dependencies; decide using one of them.");
