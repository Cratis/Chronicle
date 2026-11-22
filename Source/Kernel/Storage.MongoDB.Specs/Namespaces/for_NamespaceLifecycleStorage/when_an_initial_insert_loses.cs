// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Storage.MongoDB.Namespaces.for_NamespaceLifecycleStorage;

public class when_an_initial_insert_loses : given.bson_storage
{
    bool _won;

    void Establish() => _insertFailure = DuplicateKey();

    async Task Because() => _won = await new NamespaceLifecycleStorage(_collection).TryWrite(_name, null, new NamespaceLifecycle(_name, _identity, DateTimeOffset.UtcNow));

    [Fact] void should_report_the_lost_reservation() => _won.ShouldBeFalse();
    [Fact] void should_never_overwrite_the_winning_identity() => _replacement.ShouldBeNull();
}
