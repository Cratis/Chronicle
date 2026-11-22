// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceLifecycleStorage;

public class when_the_record_targets_another_namespace : given.bson_storage
{
    bool _won;

    async Task Because() => _won = await new NamespaceLifecycleStorage(_collection).TryWrite(_name, null, new NamespaceLifecycle("another", _identity, DateTimeOffset.UtcNow));

    [Fact] void should_refuse_the_wrong_scope() => _won.ShouldBeFalse();
    [Fact] void should_not_insert_the_record() => _inserted.ShouldBeNull();
    [Fact] void should_not_replace_another_record() => _replacement.ShouldBeNull();
}
