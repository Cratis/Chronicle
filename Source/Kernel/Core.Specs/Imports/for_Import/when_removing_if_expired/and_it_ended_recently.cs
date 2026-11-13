// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports.for_Import.when_removing_if_expired;

public class and_it_ended_recently : given.an_import
{
    bool _removed;

    async Task Establish()
    {
        await Begin();
        await _grain.Abandon("not needed");
        _time.Now += TimeSpan.FromHours(1);
    }

    async Task Because() => _removed = await _grain.RemoveIfExpired();

    [Fact] void should_keep_it() => _removed.ShouldBeFalse();
    [Fact] async Task should_not_clear_its_stored_state() => await _state.DidNotReceive().ClearStateAsync();
}
