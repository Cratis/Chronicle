// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Projections.for_CachingProjectionDefinitionsStorage.when_a_listing_overlaps_a_local_write;

public class and_the_write_deleted_it : given.a_listing_overlapping_a_local_write
{
    bool _result;

    async Task Because()
    {
        await _storage.Delete(_id);
        _inner.Release();
        await _listing;
        _result = await _storage.Has(_id);
    }

    [Fact] void should_not_bring_it_back() => _result.ShouldBeFalse();
}
