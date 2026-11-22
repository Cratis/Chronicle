// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceMarkerStorage;

public class when_the_marker_is_missing_or_mismatched : given.bson_storage
{
    bool _admitted;

    void Establish()
    {
        _matches = 0;
        _modified = 0;
    }

    async Task Because() => _admitted = await new NamespaceMarkerStorage(_collection).TryFence(_session, _identity);

    [Fact] void should_refuse_the_protected_write() => _admitted.ShouldBeFalse();
    [Fact] void should_not_recreate_the_marker() => _inserted.ShouldBeNull();
    [Fact] void should_not_upsert_a_marker() => _upsert.ShouldBeFalse();
    [Fact] void should_not_fall_back_to_a_check_then_write() => _reads.ShouldEqual(0);
}
