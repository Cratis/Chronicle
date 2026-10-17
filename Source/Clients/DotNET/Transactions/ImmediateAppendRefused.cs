// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when an immediate append is attempted in a flow that refuses it.
/// </summary>
/// <param name="operation">The public append operation.</param>
public class ImmediateAppendRefused(string operation) : Exception($"Immediate appends through {operation} are refused by the owning unit of work. Use IEventSequence.Transactional and let the owner commit the unit.");
