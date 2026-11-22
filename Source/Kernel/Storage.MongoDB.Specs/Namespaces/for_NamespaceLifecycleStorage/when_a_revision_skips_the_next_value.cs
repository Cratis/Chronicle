// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceLifecycleStorage;

public class when_a_revision_skips_the_next_value : given.bson_storage
{
    bool _won;

    async Task Because() => _won = await new NamespaceLifecycleStorage(_collection).TryWrite(_name, 41, new NamespaceLifecycle(_name, _identity, DateTimeOffset.UtcNow) { Revision = 43 });

    [Fact] void should_refuse_the_invalid_transition() => _won.ShouldBeFalse();
    [Fact] void should_not_write_anything() => _replacement.ShouldBeNull();
    [Fact] void should_not_insert_a_new_record() => _inserted.ShouldBeNull();
}
