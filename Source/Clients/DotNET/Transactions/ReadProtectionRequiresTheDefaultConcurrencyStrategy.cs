// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when a custom strategy is used on a protected-read label.
/// </summary>
/// <param name="label">The condition label.</param>
public class ReadProtectionRequiresTheDefaultConcurrencyStrategy(EventSourceId label)
    : Exception($"Read protection on '{label}' requires the default concurrency strategy.");
