// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_Reducers;

public class when_registering_a_generation_only_self_reducer : given.all_dependencies
{
    Exception? _error;

    void Establish() => _eventTypes.AllClrTypes.Returns(new EventTypesForSpecifications([typeof(Current), typeof(Previous)]).AllClrTypes);

    async Task Because() => _error = await Catch.Exception(() => _reducers.Register(typeof(Model)));

    [Fact] void should_reject_the_empty_subscription() => _error.ShouldBeOfExactType<Observation.ObserverHasOnlyGenerationHandlers>();
    [Fact] void should_name_the_self_model() => _error!.Message.ShouldContain(typeof(Model).FullName!);
    [Fact] void should_name_the_generation_type() => _error!.Message.ShouldContain(typeof(Previous).FullName!);

    [EventType(generation: 2)]
    public record Current(string Name);
    [EventTypeGenerationFor<Current>(1)]
    public record Previous(string Name);
    public record Model
    {
        public static Model Reduce(Previous @event, Model? current) => new();
    }
}
