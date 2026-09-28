// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>
/// The exception that is thrown when events are staged after a strict unit of work begins completing.
/// </summary>
/// <param name="correlationId">The correlation identifier of the completed unit of work.</param>
public class UnitOfWorkIsCompleted(CorrelationId correlationId)
    : Exception($"Events cannot be staged after unit of work '{correlationId}' begins completing.");
