// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceLifecycleStorage;

public class when_replacement_does_not_change_the_revision : given.bson_storage
{
    bool _won;

    void Establish() => _modified = 0;

    async Task Because() => _won = await new NamespaceLifecycleStorage(_collection).TryWrite(_name, 41, new NamespaceLifecycle(_name, _identity, DateTimeOffset.UtcNow) { Revision = 42 });

    [Fact] void should_not_issue_a_false_acknowledgement() => _won.ShouldBeFalse();
}
