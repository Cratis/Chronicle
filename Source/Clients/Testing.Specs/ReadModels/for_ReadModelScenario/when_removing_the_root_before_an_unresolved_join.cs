// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

public class when_removing_the_root_before_an_unresolved_join : Specification
{
    ReadModelScenario<RemovableJoinedWidget> _scenario;
    EventSourceId _widgetId;
    EventSourceId _customerId;
    Guid _customerGuid;

    void Establish()
    {
        _scenario = new ReadModelScenario<RemovableJoinedWidget>();
        _widgetId = EventSourceId.New();
        _customerGuid = Guid.NewGuid();
        _customerId = new EventSourceId(_customerGuid);
    }

    async Task Because()
    {
        await _scenario.Given
            .ForEventSource(_widgetId)
            .Events(new RemovableJoinedWidgetCreated(new JoinCustomerId(_customerGuid)), new RemovableWidgetDeleted());

        await _scenario.Given
            .ForEventSource(_customerId)
            .Events(new JoinCustomerRegistered("Ada"));
    }

    [Fact] void should_keep_the_primary_instance_absent() => _scenario.Instance.ShouldBeNull();
    [Fact] void should_keep_every_instance_absent() => _scenario.Instances.ShouldBeEmpty();
}
