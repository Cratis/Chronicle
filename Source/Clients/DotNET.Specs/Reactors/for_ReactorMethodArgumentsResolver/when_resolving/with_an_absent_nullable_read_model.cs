// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reactors.for_ReactorMethodArgumentsResolver.when_resolving;

public class with_an_absent_nullable_read_model : given.a_resolver
{
    object?[] _arguments;

    void Establish()
    {
#pragma warning disable CA2263 // The resolver calls the runtime-type overloads.
        _projections.HasFor(typeof(SomeReadModel)).Returns(true);
        _readModels.GetInstanceById(typeof(SomeReadModel), Arg.Any<ReadModelKey>()).Returns(Task.FromResult<object>(null!));
#pragma warning restore CA2263
    }

    async Task Because() => _arguments = await _resolver.Resolve(
        typeof(ReactorWithNullableReadModel).GetMethod(nameof(ReactorWithNullableReadModel.Handle))!,
        new ReactorWithNullableReadModel(),
        _event,
        _eventContext,
        _eventStore,
        _serviceProvider);

    [Fact] void should_pass_null_for_the_absent_read_model() => _arguments[1].ShouldBeNull();
}
