// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients.for_ReducerDefinitionComparer.when_comparing;

public class and_filter_collections_are_recreated : given.two_definitions
{
    async Task Because() => _result = await _comparer.Compare(_key, _previous, _current);

    [Fact] void should_recognize_the_same_definition() => _result.ShouldEqual(ReducerDefinitionCompareResult.Same);
}
