// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// Exception that gets thrown when a unit of work is already rolled back.
/// </summary>
/// <param name="correlationId">Correlation identifier for the unit of work.</param>
public class UnitOfWorkIsAlreadyRolledBack(CorrelationId correlationId) : Exception($"Unit of work with correlation identifier '{correlationId}' is already rolled back");
