// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reactors.for_ReactorDefinitionComparer.when_comparing.and_the_local_definition_is_empty;

public class and_the_stored_definition_matches : given.a_reactor_definition_comparer
{
    void Establish()
    {
        _reactorsStorage.Get(_key.ReactorId).Returns(_second);
        _first = EmptyState();
    }

    async Task Because() => await Compare();

    [Fact] void should_be_same() => _result.ShouldEqual(ReactorDefinitionCompareResult.Same);
    [Fact] async Task should_read_the_stored_definition() => await _reactorsStorage.Received(1).Get(_key.ReactorId);
}
