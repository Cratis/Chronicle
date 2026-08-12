// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when a unit of work receives conflicting explicit concurrency scopes for one label.
/// </summary>
/// <param name="scopeLabel">The label that already has an explicit scope.</param>
/// <param name="enrolledScope">The previously enrolled scope.</param>
/// <param name="attemptedScope">The conflicting scope that was attempted.</param>
public class ConflictingConcurrencyScopesForLabel(
    EventSourceId scopeLabel,
    ConcurrencyScope enrolledScope,
    ConcurrencyScope attemptedScope)
    : Exception($"Concurrency scope label '{scopeLabel}' is already enrolled with scope '{enrolledScope}' and cannot be changed to '{attemptedScope}'.");
