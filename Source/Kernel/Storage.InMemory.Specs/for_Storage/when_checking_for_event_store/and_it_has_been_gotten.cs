// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.InMemory.for_Storage.when_checking_for_event_store;

public class and_it_has_been_gotten : given.a_storage
{
    static readonly EventStoreName _eventStore = "SomeEventStore";
    bool _before;
    bool _after;

    async Task Because()
    {
        _before = await _storage.HasEventStore(_eventStore);
        _storage.GetEventStore(_eventStore);
        _after = await _storage.HasEventStore(_eventStore);
    }

    [Fact] void should_not_have_it_before_getting() => _before.ShouldBeFalse();
    [Fact] void should_have_it_after_getting() => _after.ShouldBeTrue();
}
