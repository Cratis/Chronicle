// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Events.Constraints.for_ClosedStreamsConstraintStorage.when_reopening;

public class and_owner_differs : given.a_closed_streams_storage
{
    bool _removed;

    async Task Because() => _removed = await _storage.Reopen("constraint", _closure.Scope);

    [Fact] void should_not_remove_another_owners_closure() => _removed.ShouldBeFalse();
    [Fact] async Task should_keep_the_manual_closure() => (await _storage.GetAll()).ShouldContainOnly(_closure);
}
