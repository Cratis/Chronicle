// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reactors.for_ReactorMethodArgumentsResolver.when_resolving;

#pragma warning disable CA2263 // Prefer generic overload when type is known - the resolver works off the runtime Type
public class with_a_read_model_from_a_reducer : given.a_resolver
{
    SomeReadModel _readModel;
    object?[] _arguments;

    void Establish()
    {
        _readModel = new SomeReadModel();
        _projections.HasFor(typeof(SomeReadModel)).Returns(false);
        _reducers.HasFor(typeof(SomeReadModel)).Returns(true);
        _readModels.GetInstanceById(typeof(SomeReadModel), Arg.Any<ReadModelKey>()).Returns(_readModel);
    }

    async Task Because() => _arguments = await _resolver.Resolve(
        MethodNamed(nameof(ReactorWithDependencies.WithReadModel)),
        new ReactorWithDependencies(),
        _event,
        _eventContext,
        _eventStore,
        _serviceProvider);

    [Fact] void should_materialize_the_read_model() => _arguments[1].ShouldEqual(_readModel);
}
#pragma warning restore CA2263
