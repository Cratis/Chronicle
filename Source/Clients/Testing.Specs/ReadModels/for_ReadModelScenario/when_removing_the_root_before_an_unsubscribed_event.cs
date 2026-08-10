// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario;

public class when_removing_the_root_before_an_unsubscribed_event : Specification
{
    ReadModelScenario<RemovableWidget> _scenario;
    EventSourceId _widgetId;

    void Establish()
    {
        _scenario = new ReadModelScenario<RemovableWidget>();
        _widgetId = EventSourceId.New();
    }

    async Task Because() =>
        await _scenario.Given
            .ForEventSource(_widgetId)
            .Events(new RemovableWidgetCreated("My Widget"), new RemovableWidgetDeleted(), new ThingTouched("Ignored"));

    [Fact] void should_keep_the_primary_instance_absent() => _scenario.Instance.ShouldBeNull();
    [Fact] void should_keep_every_instance_absent() => _scenario.Instances.ShouldBeEmpty();
}
