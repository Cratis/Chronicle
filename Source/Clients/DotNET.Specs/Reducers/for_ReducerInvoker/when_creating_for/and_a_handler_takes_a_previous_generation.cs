// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerInvoker.when_creating_for;

public class and_a_handler_takes_a_previous_generation : Specification
{
    ReducerInvoker _invoker;

    void Because() => _invoker = new(
        new EventTypesForSpecifications([typeof(Previous), typeof(Current)]),
        Substitute.For<IClientArtifactsActivator>(),
        typeof(Reducer),
        typeof(Model),
        nameof(Model));

    [Fact] void should_not_register_the_handler() => _invoker.EventTypes.ShouldBeEmpty();

    [EventTypeGenerationFor<Current>(1)]
    public record Previous(string Name);

    [EventType(generation: 2)]
    public record Current(string Name);

    public record Model(string Name);

    public class Reducer : IReducerFor<Model>
    {
        public Model Handle(Previous @event, Model? current) => new(@event.Name);
    }
}
