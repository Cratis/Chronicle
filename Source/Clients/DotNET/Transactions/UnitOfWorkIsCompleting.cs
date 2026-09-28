// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when a unit of work refuses rollback while its commit is in progress.
/// </summary>
/// <param name="correlationId">The correlation identifier of the unit of work being committed.</param>
public class UnitOfWorkIsCompleting(CorrelationId correlationId)
    : Exception($"Unit of work '{correlationId}' cannot be rolled back while a commit is in progress.");
