// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Projections.for_CachingProjectionDefinitionsStorage;

public class when_getting_a_definition_after_deleting : given.a_caching_projection_definitions_storage
{
    async Task Establish()
    {
        _inner.Get(_id).Returns(_definition);
        await _storage.Get(_id);
    }

    async Task Because()
    {
        await _storage.Delete(_id);
        await _storage.Get(_id);
    }

    [Fact] void should_delete_from_inner() => _inner.Received(1).Delete(_id);
    [Fact] void should_delegate_to_inner_again_after_eviction() => _inner.Received(2).Get(_id);
}
