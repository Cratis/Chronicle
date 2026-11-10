// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports.for_Import.when_removing_if_expired;

public class and_it_ended_longer_ago_than_the_retention : given.an_import
{
    bool _removed;

    async Task Establish()
    {
        await Begin();
        await _grain.Abandon("not needed");
        _time.Now += TimeSpan.FromHours(25);
    }

    async Task Because() => _removed = await _grain.RemoveIfExpired();

    [Fact] void should_remove_it() => _removed.ShouldBeTrue();
    [Fact] async Task should_clear_its_stored_state() => await _state.Received(1).ClearStateAsync();
    [Fact] void should_forget_it() => _grainState.Record.ShouldBeNull();
}
