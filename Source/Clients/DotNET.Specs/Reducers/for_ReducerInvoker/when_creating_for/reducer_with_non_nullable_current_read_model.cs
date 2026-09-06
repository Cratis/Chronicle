// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reducers.Validators;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reducers.for_ReducerInvoker.when_creating_for;

/// <summary>
/// Regression for https://github.com/Cratis/Chronicle/issues/3947 — a handler declaring a non-nullable current
/// read model used to be dropped from dispatch without a word, leaving the reducer looking registered while none
/// of its events were ever applied.
/// </summary>
public class reducer_with_non_nullable_current_read_model : Specification
{
    IClientArtifactsActivator _artifactActivator;
    IEventTypes _eventTypes;
    ReducerInvoker _invoker;
    Exception _result;

    void Establish()
    {
        _artifactActivator = Substitute.For<IClientArtifactsActivator>();
        _artifactActivator.Activate(Arg.Any<IServiceProvider>(), typeof(ReducerWithNonNullableCurrentReadModel))
            .Returns(callInfo => new ActivatedArtifact(
                new ReducerWithNonNullableCurrentReadModel(),
                typeof(ReducerWithNonNullableCurrentReadModel),
                Substitute.For<ILogger<ActivatedArtifact>>()));
        _eventTypes = new EventTypesForSpecifications([]);
    }

    void Because() => _result = Catch.Exception(() => _invoker = new ReducerInvoker(
        _eventTypes,
        _artifactActivator,
        typeof(ReducerWithNonNullableCurrentReadModel),
        typeof(ReadModel),
        nameof(ReadModel)));

    [Fact] void should_fail() => _result.ShouldNotBeNull();
    [Fact] void should_not_create_the_invoker() => _invoker.ShouldBeNull();
    [Fact] void should_say_the_current_read_model_must_be_nullable() => _result.ShouldBeOfExactType<ReducerMethodCurrentReadModelMustBeNullable>();
    [Fact] void should_name_the_offending_method() => _result.Message.ShouldContain(nameof(ReducerWithNonNullableCurrentReadModel.Reduce));
}
