// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.for_ReactorBuilder.when_building;

public class without_handlers : given.a_reactor_builder
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _builder.Build("empty"));

    [Fact] void should_fail() => _error.ShouldBeOfExactType<NoEventTypesForReactor>();
}
