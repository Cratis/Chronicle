// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_registering_generation_only_handlers : given.all_dependencies
{
    Exception? _error;

    void Establish() => _eventTypes.AllClrTypes.Returns(new EventTypesForSpecifications([typeof(Current), typeof(Previous)]).AllClrTypes);

    async Task Because() => _error = await Catch.Exception(_reducers.Register<GenerationOnlyReducer, Model>);

    [Fact] void should_fail_before_registering_an_empty_observer() => _error.ShouldBeOfExactType<Observation.ObserverHasOnlyGenerationHandlers>();
    [Fact] void should_name_the_artifact() => _error!.Message.ShouldContain(typeof(GenerationOnlyReducer).FullName!);
    [Fact] void should_name_the_generation_only_handler_type() => _error!.Message.ShouldContain(typeof(Previous).FullName!);
    [Fact] void should_explain_generation_pinning() => _error!.Message.ShouldContain("pin");

    [EventType(generation: 2)]
    public record Current(string Name);
    [EventTypeGenerationFor<Current>(1)]
    public record Previous(string Name);
    public record Model;
    public class GenerationOnlyReducer : IReducerFor<Model>
    {
        public Model Handle(Previous @event, Model? current) => new();
    }
}
