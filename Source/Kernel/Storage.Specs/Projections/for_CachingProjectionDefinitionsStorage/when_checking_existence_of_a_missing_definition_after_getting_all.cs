// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Projections.for_CachingProjectionDefinitionsStorage;

public class when_checking_existence_of_a_missing_definition_after_getting_all : given.a_caching_projection_definitions_storage
{
    bool _result;

    async Task Establish()
    {
        _inner.GetAll().Returns([]);
        _inner.Has(_id).Returns(false);
        await _storage.GetAll();
    }

    async Task Because() => _result = await _storage.Has(_id);

    [Fact] async Task should_ask_the_shared_storage() => await _inner.Received(1).Has(_id);
    [Fact] void should_not_have_it() => _result.ShouldBeFalse();
}
