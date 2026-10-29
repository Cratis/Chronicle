// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerDefinitionComparer.when_comparing.and_the_local_definition_is_empty;

public class and_the_stored_definition_matches : given.two_definitions
{
    void Establish()
    {
        _reducersStorage.Get(_key.ReducerId).Returns(_previous);
        _previous = _previous with { EventTypes = null! };
    }

    async Task Because() => _result = await _comparer.Compare(_key, _previous, _current);

    [Fact] void should_be_same() => _result.ShouldEqual(ReducerDefinitionCompareResult.Same);
    [Fact] async Task should_read_the_stored_definition() => await _reducersStorage.Received(1).Get(_key.ReducerId);
}
