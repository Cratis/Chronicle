// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerHandlerKernelObservation;

public class when_checking_kernel_observation : Specification
{
    bool _activeWithEvents;
    bool _inactiveWithEvents;
    bool _activeWithoutEvents;
    bool _inactiveWithoutEvents;

    void Because()
    {
        _activeWithEvents = Handler(true, [EventType.Unknown]).IsObservedByKernel();
        _inactiveWithEvents = Handler(false, [EventType.Unknown]).IsObservedByKernel();
        _activeWithoutEvents = Handler(true, []).IsObservedByKernel();
        _inactiveWithoutEvents = Handler(false, []).IsObservedByKernel();
    }

    static IReducerHandler Handler(bool active, EventType[] events)
    {
        var handler = Substitute.For<IReducerHandler>();
        handler.IsActive.Returns(active);
        handler.EventTypes.Returns(events);
        return handler;
    }

    [Fact] void should_observe_active_handlers_with_events() => _activeWithEvents.ShouldBeTrue();
    [Fact] void should_not_observe_inactive_handlers_with_events() => _inactiveWithEvents.ShouldBeFalse();
    [Fact] void should_not_observe_active_handlers_without_events() => _activeWithoutEvents.ShouldBeFalse();
    [Fact] void should_not_observe_inactive_handlers_without_events() => _inactiveWithoutEvents.ShouldBeFalse();
}
