// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>Exception that gets thrown when completion of a unit of work has already been claimed.</summary>
/// <param name="correlationId">The <see cref="CorrelationId"/> of the unit of work.</param>
public class UnitOfWorkCompletionAlreadyClaimed(CorrelationId correlationId)
    : Exception($"Completion of the unit of work with correlation identifier '{correlationId}' has already been claimed");
