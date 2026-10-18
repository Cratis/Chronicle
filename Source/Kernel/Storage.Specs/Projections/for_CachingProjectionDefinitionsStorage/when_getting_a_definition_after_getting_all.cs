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
        _inner.Get(_id).Returns(_definition);
        await _storage.GetAll();
    }

    async Task Because() => _result = await _storage.Get(_id);

    [Fact] void should_read_shared_storage_even_after_a_listing() => _inner.Received(1).Get(_id);
    [Fact] void should_return_the_shared_definition() => _result.ShouldEqual(_definition);
}
