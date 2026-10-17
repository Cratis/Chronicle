// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Storage.Projections.for_CachingProjectionDefinitionsStorage.when_a_listing_overlaps_a_local_write;

public class and_the_write_saved_a_change : given.a_listing_overlapping_a_local_write
{
    ProjectionDefinition _result;

    async Task Because()
    {
        await _storage.Save(_changed);
        _inner.Release();
        await _listing;
        _result = await _storage.Get(_id);
    }

    [Fact] void should_keep_the_saved_change() => _result.ShouldEqual(_changed);
}
