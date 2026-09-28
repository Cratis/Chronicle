// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reactors.for_ReactorBuilder.when_building;

public class with_a_handler_for_a_type_that_is_not_an_event : given.a_reactor_builder
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _builder.On<NotAnEvent>(_ => { }));

    [Fact] void should_fail() => _error.ShouldBeOfExactType<TypeIsNotAnEventType>();
}
