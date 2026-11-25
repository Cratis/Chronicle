// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.given;

/// <summary>
/// A keeper check that confirms a subscription from the observer, with the call to the observer's client grain counted.
/// </summary>
public class a_confirmation_from_the_observer : Specification
{
    protected IObserver _observer;
    protected ConnectionId _connectionId;
    protected int _grainCalls;
    protected bool _grainAnswer = true;
    protected bool _intact;

    void Establish()
    {
        _connectionId = new ConnectionId("a-connection");
        _observer = Substitute.For<IObserver>();
    }

    protected async Task Confirm() => _intact = await ClientSubscriptionKeeper.ConfirmFromObserver(
        () =>
        {
            _grainCalls++;
            return Task.FromResult(_grainAnswer);
        },
        _observer,
        _connectionId)();

    protected void ObserverKnowsTheClient(bool knows) =>
        _observer.KnowsClient(_connectionId).Returns(Task.FromResult(knows));
}
