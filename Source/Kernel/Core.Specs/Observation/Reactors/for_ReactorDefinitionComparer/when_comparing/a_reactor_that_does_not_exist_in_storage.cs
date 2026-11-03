// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Observation.Reactors.for_ReactorDefinitionComparer.when_comparing;

public class a_reactor_that_does_not_exist_in_storage : given.a_reactor_definition_comparer
{
    void Establish() => _reactorsStorage.Has(_key.ReactorId).Returns(false);

    async Task Because() => await Compare();

    [Fact] void should_be_new() => _result.ShouldEqual(ReactorDefinitionCompareResult.New);
    [Fact] async Task should_not_read_the_stored_definition() => await _reactorsStorage.DidNotReceive().Get(Arg.Any<ReactorId>());
}
