// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events.given;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_processing_appended_events;

public class and_joining_events_from_another_source : appended_contexts
{
    ReadModelScenario<JoinOrderSummary> _scenario;
    Guid _orderId;
    Guid _customerId;

    void Establish()
    {
        _scenario = new();
        _orderId = Guid.NewGuid();
        _customerId = Guid.NewGuid();
    }

    Task Because() => _scenario.ProcessAppendedEvents(
    [
        new AppendedEvent(_context with { EventSourceId = new(_orderId), SequenceNumber = 7UL }, new JoinOrderPlaced(new(_customerId), 100m)),
        new AppendedEvent(_context with { EventSourceId = new(_customerId) }, new JoinCustomerRegistered("Ada"))
    ]);
    void Destroy() => _scenario.Dispose();

    [Fact] void should_materialize_only_the_order() => _scenario.Instances.Count.ShouldEqual(1);
    [Fact] void should_preserve_the_joined_name() => _scenario.InstanceForEventSourceId(new(_orderId))!.CustomerName.ShouldEqual("Ada");
    [Fact] void should_preserve_the_order_key() => _scenario.InstanceForEventSourceId(new(_orderId))!.Id.ShouldEqual(_orderId);
}
