// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerDefinitionComparer.when_comparing;

public class and_only_the_tombstone_marker_changed : given.two_definitions
{
    void Establish() => _current = _current with
    {
        EventTypes = _current.EventTypes.Select(_ => _ with { EventType = _.EventType with { Tombstone = true } }).ToArray()
    };

    async Task Because() => _result = await _comparer.Compare(_key, _previous, _current);

    [Fact] void should_be_the_same() => _result.ShouldEqual(ReducerDefinitionCompareResult.Same);
}
