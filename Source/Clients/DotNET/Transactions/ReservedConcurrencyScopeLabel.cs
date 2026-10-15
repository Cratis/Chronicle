// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when a caller uses a label reserved for read protection.
/// </summary>
/// <param name="label">The reserved label.</param>
public class ReservedConcurrencyScopeLabel(EventSourceId label)
    : Exception($"Concurrency scope label '{label}' is reserved for read protection; use a label not starting with '$read-protection/'.");
