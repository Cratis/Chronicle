// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// The exception that is thrown when an independent concurrency-scope label cannot be validated explicitly.
/// </summary>
/// <param name="scopeLabel">The independent label whose scope is not explicit.</param>
public class IndependentConcurrencyScopeMustBeExplicit(EventSourceId scopeLabel)
    : Exception($"Independent concurrency scope label '{scopeLabel}' must use a concrete exact scope or ConcurrencyScope.None.");
