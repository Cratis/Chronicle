// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Transactions;

public interface IUnitOfWorkEnlistee
{
    CorrelationId CorrelationId { get; }
    EventSourceId EventSourceId { get; }
    EventSequenceNumber ExpectedEventSequenceNumber { get; }

    ValueTask Complete();
}
