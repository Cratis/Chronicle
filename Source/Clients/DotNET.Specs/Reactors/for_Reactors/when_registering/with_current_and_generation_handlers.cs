// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering;

public class with_current_and_generation_handlers : given.all_dependencies
{
    IReactorHandler _handler;

    void Establish() => _eventStore.EventTypes.Returns(new EventTypesForSpecifications([typeof(Current), typeof(Previous)]));

    async Task Because() => _handler = await _reactors.Register<MixedReactor>();

    [Fact] void should_observe_only_the_current_event_type() => _handler.EventTypes.ShouldContainOnly(typeof(Current).GetEventType());

    [EventType(generation: 2)]
    public record Current(string Name);
    [EventTypeGenerationFor<Current>(1)]
    public record Previous(string Name);
    public class MixedReactor : IReactor
    {
        public Task Handle(Current @event) => Task.CompletedTask;
        public Task Handle(Previous @event) => Task.CompletedTask;
    }
}
