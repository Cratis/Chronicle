// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_EventSequencePage;

public class when_the_window_is_empty : given.a_page_query
{
    async Task Because() => _page = await Query(from: 100);

    [Fact] void should_return_no_events() => _page.Events.ShouldBeEmpty();
    [Fact] void should_return_no_continuation() => _page.Next.ShouldBeNull();
    [Fact] void should_not_release_any_content() => _released.ShouldBeEmpty();
    [Fact] void should_dispose_the_cursor() => _cursor.Received(1).Dispose();
}
