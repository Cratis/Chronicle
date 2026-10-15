// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ClosesStreamIndexUpdater.when_updating;

public class and_event_reopens : given.a_closing_index
{
    async Task Establish()
    {
        await _storage.Close(new(_scope, "closing", EventSequenceNumber.First, null));
        await _storage.Close(new(_scope, ClosedStreamOwner.Manual, EventSequenceNumber.First, null));
        await _storage.Close(new(_scope, "other", EventSequenceNumber.First, null));
    }

    async Task Because() => await new ClosesStreamIndexUpdater(_definition, ContextFor("Reopened"), _storage).Update(new EventSequenceNumber(5));

    [Fact] async Task should_remove_only_its_owned_scope() => (await _storage.GetForOwner("closing")).ShouldBeEmpty();
    [Fact] async Task should_keep_manual_closures() => (await _storage.GetForOwner(ClosedStreamOwner.Manual)).Count().ShouldEqual(1);
    [Fact] async Task should_keep_other_owners() => (await _storage.GetForOwner("other")).Count().ShouldEqual(1);
}
