// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerDefinitionComparer.when_comparing;

public class a_reducer_that_does_not_exist_in_storage : given.two_definitions
{
    void Establish()
    {
        _reducersStorage.Has(_key.ReducerId).Returns(false);
        _previous = _previous with { EventTypes = null! };
    }

    async Task Because() => _result = await _comparer.Compare(_key, _previous, _current);

    [Fact] void should_be_new() => _result.ShouldEqual(ReducerDefinitionCompareResult.New);
    [Fact] async Task should_not_read_the_stored_definition() => await _reducersStorage.DidNotReceive().Get(Arg.Any<ReducerId>());
}
