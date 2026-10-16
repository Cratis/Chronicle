// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when an immediate append occurs inside a protected invocation.
/// </summary>
/// <param name="operation">The public append operation.</param>
/// <param name="description">The event or operation description.</param>
public class ImmediateAppendWhileReadsAreProtected(string operation, string description) : Exception($"The command appended {description} immediately through {operation} while read protection is On. Return the event from Handle, or use IEventLog.Transactional.");
