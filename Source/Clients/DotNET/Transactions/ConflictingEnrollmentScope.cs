// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when an enrollment cannot carry its required concurrency scope.
/// </summary>
/// <param name="eventSourceId">The event source being enrolled.</param>
/// <param name="effective">The effective scope.</param>
/// <param name="required">The required scope that conflicts with the enrollment.</param>
public class ConflictingEnrollmentScope(EventSourceId eventSourceId, ConcurrencyScope effective, ConcurrencyScope required)
    : Exception($"Enrollment for event source '{eventSourceId}' conflicts with its required scope. Effective: '{effective}'; required: '{required}'.");
