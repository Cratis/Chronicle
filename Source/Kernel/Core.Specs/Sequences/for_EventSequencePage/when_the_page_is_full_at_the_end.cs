// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_EventSequencePage;

public class when_the_page_is_full_at_the_end : given.a_page_query
{
    void Establish() => _window = [EventAt(0), EventAt(ulong.MaxValue)];

    async Task Because() => _page = await Query();

    [Fact] void should_return_the_entire_page() => _page.Events.Count().ShouldEqual(2);
    [Fact] void should_not_invent_a_continuation_or_overflow() => _page.Next.ShouldBeNull();
}
