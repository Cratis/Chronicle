// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.Reactors.for_ReactorDefinitionComparer.when_comparing.and_the_local_definition_is_empty;

public class and_the_stored_definition_differs : given.a_reactor_definition_comparer
{
    void Establish()
    {
        _reactorsStorage.Get(_key.ReactorId).Returns(_first);
        _first = EmptyState();
        _second = DefinitionWith(new EventType("OtherEvent", EventTypeGeneration.First));
    }

    async Task Because() => await Compare();

    [Fact] void should_be_different() => _result.ShouldEqual(ReactorDefinitionCompareResult.Different);
}
