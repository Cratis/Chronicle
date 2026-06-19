// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reactors.for_ReactorMethodArgumentsResolver.when_resolving;

#pragma warning disable CA2263 // Prefer generic overload when type is known - the resolver works off the runtime Type
public class with_a_read_model : given.a_resolver
{
    SomeReadModel _readModel;
    object?[] _arguments;

    void Establish()
    {
        _readModel = new SomeReadModel();
        _projections.HasFor(typeof(SomeReadModel)).Returns(true);
        _readModels.GetInstanceById(typeof(SomeReadModel), Arg.Any<ReadModelKey>()).Returns(_readModel);
    }

    async Task Because() => _arguments = await _resolver.Resolve(
        MethodNamed(nameof(ReactorWithDependencies.WithReadModel)),
        new ReactorWithDependencies(),
        _event,
        _eventContext,
        _eventStore,
        _serviceProvider);

    [Fact] void should_pass_the_event_as_the_first_argument() => _arguments[0].ShouldEqual(_event);
    [Fact] void should_materialize_the_read_model() => _arguments[1].ShouldEqual(_readModel);
    [Fact] void should_materialize_using_the_event_source_id_as_key() => _readModels.Received(1).GetInstanceById(typeof(SomeReadModel), (ReadModelKey)_eventSourceId);
}
#pragma warning restore CA2263
