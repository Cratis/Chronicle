// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reactors.for_ReactorMethodArgumentsResolver.when_resolving;

#pragma warning disable CA2263 // Prefer generic overload when type is known - the resolver works off the runtime Type
public class with_mixed_dependencies : given.a_resolver
{
    SomeReadModel _readModel;
    ISomeService _service;
    object?[] _arguments;

    void Establish()
    {
        _readModel = new SomeReadModel();
        _service = Substitute.For<ISomeService>();
        _projections.HasFor(typeof(SomeReadModel)).Returns(true);
        _readModels.GetInstanceById(typeof(SomeReadModel), Arg.Any<ReadModelKey>()).Returns(_readModel);
        _serviceProvider.GetService(typeof(ISomeService)).Returns(_service);
    }

    async Task Because() => _arguments = await _resolver.Resolve(
        MethodNamed(nameof(ReactorWithDependencies.Mixed)),
        new ReactorWithDependencies(),
        _event,
        _eventContext,
        _eventStore,
        _serviceProvider);

    [Fact] void should_pass_the_event_as_the_first_argument() => _arguments[0].ShouldEqual(_event);
    [Fact] void should_pass_the_event_context_as_the_second_argument() => _arguments[1].ShouldEqual(_eventContext);
    [Fact] void should_materialize_the_read_model_as_the_third_argument() => _arguments[2].ShouldEqual(_readModel);
    [Fact] void should_resolve_the_service_as_the_fourth_argument() => _arguments[3].ShouldEqual(_service);
}
#pragma warning restore CA2263
