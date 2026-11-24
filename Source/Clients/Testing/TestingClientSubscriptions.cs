// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;

using KernelClientReactorSubscription = KernelCore::Cratis.Chronicle.Observation.Reactors.Clients.IClientReactorSubscription;
using KernelClientReactorSubscriptions = KernelCore::Cratis.Chronicle.Observation.Reactors.Clients.IClientReactorSubscriptions;
using KernelClientReducerSubscription = KernelCore::Cratis.Chronicle.Observation.Reducers.Clients.IClientReducerSubscription;
using KernelClientReducerSubscriptions = KernelCore::Cratis.Chronicle.Observation.Reducers.Clients.IClientReducerSubscriptions;
using KernelConnectedObserverKey = KernelConcepts::Cratis.Chronicle.Concepts.Observation.ConnectedObserverKey;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// The client reactor and reducer subscriptions of a test scenario, which has no silo for a connection to subscribe through.
/// </summary>
internal sealed class TestingClientSubscriptions : KernelClientReactorSubscriptions, KernelClientReducerSubscriptions
{
    /// <inheritdoc/>
    KernelClientReactorSubscription KernelClientReactorSubscriptions.Create(KernelConnectedObserverKey key) =>
        throw new GrainNotAvailableInTestScenario($"A client reactor subscription for '{key}'");

    /// <inheritdoc/>
    KernelClientReducerSubscription KernelClientReducerSubscriptions.Create(KernelConnectedObserverKey key) =>
        throw new GrainNotAvailableInTestScenario($"A client reducer subscription for '{key}'");
}
