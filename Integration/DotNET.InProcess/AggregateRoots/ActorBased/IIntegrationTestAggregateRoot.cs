// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Orleans.Aggregates;

namespace Cratis.Chronicle.InProcess.Integration.AggregateRoots.ActorBased;

public interface IIntegrationTestAggregateRoot<TInternalState> : IAggregateRoot
    where TInternalState : class
{
    Task<TInternalState> GetState();
    Task<CorrelationId> GetCorrelationId();
    Task<bool> GetIsNew();
}
