// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ReindexConstraintsStep.when_performing;

public class and_reopen_follows_close : for_ReindexConstraintsStep.given.a_closing_constraint_reindex_step
{
    void Establish()
    {
        _events.Add(Event("Closed", 5, Payload("period", "April")));
        _events.Add(Event("Reopened", 6, Payload("period", "April")));
    }

    async Task Because() => await Perform();

    [Fact] void should_complete_without_an_exception() => _result.TryGetException(out _).ShouldBeFalse();
    [Fact] async Task should_keep_the_reopened_scope_open() => (await _rows.GetForOwner("closing")).ShouldBeEmpty();
}
