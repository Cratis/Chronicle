// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Projections.for_CachingProjectionDefinitionsStorage;

public class when_checking_existence_after_getting : given.a_caching_projection_definitions_storage
{
    bool _result;

    async Task Establish()
    {
        _inner.Get(_id).Returns(_definition);
        _inner.Has(_id).Returns(true);
        await _storage.Get(_id);
    }

    async Task Because() => _result = await _storage.Has(_id);

    [Fact] void should_check_shared_storage() => _inner.Received(1).Has(_id);
    [Fact] void should_find_the_shared_definition() => _result.ShouldBeTrue();
}
