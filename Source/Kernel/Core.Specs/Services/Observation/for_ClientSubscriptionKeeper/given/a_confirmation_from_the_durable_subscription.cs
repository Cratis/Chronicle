// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.for_ClientSubscriptionKeeper.given;

/// <summary>
/// A keeper check that confirms a subscription from the durable subscription, with the call to the observer's client grain counted.
/// </summary>
public class a_confirmation_from_the_durable_subscription : Specification
{
    protected IDormantObservers _dormantObservers;
    protected int _grainCalls;
    protected bool _grainAnswer = true;
    protected bool _intact;
    protected ConnectedObserverKey _key;

    void Establish()
    {
        _key = new ConnectedObserverKey("an-observer", "an-event-store", "a-namespace", "a-sequence", new ConnectionId("a-connection"));
        _dormantObservers = Substitute.For<IDormantObservers>();
    }

    protected async Task Confirm() => _intact = await ClientSubscriptionKeeper.ConfirmFromDurableSubscription(
        () =>
        {
            _grainCalls++;
            return Task.FromResult(_grainAnswer);
        },
        _dormantObservers,
        _key)();

    protected void ObserverHoldsTheClient(bool holds) =>
        _dormantObservers.HoldsClient(Arg.Any<ObserverKey>(), _key.ConnectionId).Returns(Task.FromResult(holds));
}
