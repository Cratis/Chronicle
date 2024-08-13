// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Exception that gets thrown when a unit of work is already committed.
/// </summary>
/// <param name="correlationId">The <see cref="CorrelationId"/> for the unit of work.</param>
public class UnitOfWorkIsAlreadyCommitted(CorrelationId correlationId) : Exception($"Unit of work with correlation identifier '{correlationId}' is already committed");
