// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceMarkerStorage;

public class when_the_conflict_write_does_not_modify_the_marker : given.bson_storage
{
    bool _admitted;

    void Establish() => _modified = 0;

    async Task Because() => _admitted = await new NamespaceMarkerStorage(_collection).TryFence(_session, _identity);

    [Fact] void should_not_treat_a_noop_as_a_real_fence() => _admitted.ShouldBeFalse();
}
