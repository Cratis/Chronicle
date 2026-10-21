// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering;

public class with_generation_only_handlers_and_pinned_delivery : given.all_dependencies
{
    Exception? _error;
    protected override Observation.EventGenerationDelivery GenerationDelivery => Observation.EventGenerationDelivery.Pinned;

    void Establish() => _eventStore.EventTypes.Returns(new EventTypesForSpecifications([typeof(Current)]));

    async Task Because() => _error = await Catch.Exception(_reactors.Register<GenerationOnlyReactor>);

    [Fact] void should_reject_generation_handlers_even_if_the_archived_type_is_not_registered() => _error.ShouldBeOfExactType<Observation.ObserverHasOnlyGenerationHandlers>();
    [Fact] void should_identify_the_unregistered_generation_type() => _error!.Message.ShouldContain(typeof(Previous).FullName!);

    [EventType(generation: 2)]
    public record Current(string Name);
    [EventTypeGenerationFor<Current>(1)]
    public record Previous(string Name);
    public class GenerationOnlyReactor : IReactor
    {
        public Task Handle(Previous @event) => Task.CompletedTask;
    }
}
