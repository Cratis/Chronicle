// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_borrowing_an_event_store;

public class and_joining_across_event_sources : a_borrowed_store
{
    ReadModelScenario<JoinOrderSummary> _borrowed;
    ReadModelScenario<JoinOrderSummary> _fresh;
    ReadModelScenario<JoinOrderSummary> _other;
    EventSourceId _orderId;
    EventSourceId _otherOrderId;
    JoinCustomerId _customerId;

    void Establish()
    {
        _borrowed = Borrowed<JoinOrderSummary>();
        _fresh = Fresh<JoinOrderSummary>();
        _other = Borrowed<JoinOrderSummary>();
        _orderId = new(Guid.NewGuid());
        _otherOrderId = new(Guid.NewGuid());
        _customerId = new(Guid.NewGuid());
    }

    async Task Because()
    {
        foreach (var scenario in new[] { _borrowed, _fresh })
        {
            await scenario.Given.ForEventSource(new EventSourceId(_customerId.Value)).Events(new JoinCustomerRegistered("Ada"));
            await scenario.Given.ForEventSource(_orderId).Events(new JoinOrderPlaced(_customerId, 100m));
        }

        await _other.Given.ForEventSource(_otherOrderId).Events(new JoinOrderPlaced(_customerId, 200m));
    }

    [Fact] void should_resolve_the_join_from_its_own_history() => _borrowed.Instance!.CustomerName.ShouldEqual("Ada");
    [Fact] void should_match_a_fresh_scenario() => Json(_borrowed.Instances).ShouldEqual(Json(_fresh.Instances));
    [Fact] void should_not_join_with_another_scenarios_history() => string.IsNullOrEmpty(_other.Instance!.CustomerName).ShouldBeTrue();
    [Fact] void should_keep_the_other_scenarios_order() => _other.Instance!.Amount.ShouldEqual(200m);
}
