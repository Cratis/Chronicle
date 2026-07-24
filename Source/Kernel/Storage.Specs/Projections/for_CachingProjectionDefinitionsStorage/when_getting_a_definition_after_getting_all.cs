// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.Projections.for_CachingProjectionDefinitionsStorage;

public class when_getting_a_definition_after_getting_all : given.a_caching_projection_definitions_storage
{
    ProjectionDefinition _result;

    async Task Establish()
    {
        _inner.GetAll().Returns([_definition]);
        await _storage.GetAll();
    }

    async Task Because() => _result = await _storage.Get(_id);

    [Fact] void should_not_ask_inner_for_the_definition() => _inner.DidNotReceive().Get(_id);
    [Fact] void should_return_the_definition_from_cache() => _result.ShouldEqual(_definition);
}
