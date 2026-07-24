// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.Projections.for_CachingProjectionDefinitionsStorage;

public class when_getting_the_same_definition_twice : given.a_caching_projection_definitions_storage
{
    ProjectionDefinition _first;
    ProjectionDefinition _second;

    void Establish() => _inner.Get(_id).Returns(_definition);

    async Task Because()
    {
        _first = await _storage.Get(_id);
        _second = await _storage.Get(_id);
    }

    [Fact] void should_only_ask_inner_once() => _inner.Received(1).Get(_id);
    [Fact] void should_return_the_definition_the_first_time() => _first.ShouldEqual(_definition);
    [Fact] void should_return_the_same_definition_the_second_time() => _second.ShouldEqual(_definition);
}
