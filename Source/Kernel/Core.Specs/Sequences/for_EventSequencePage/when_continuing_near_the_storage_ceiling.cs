// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_EventSequencePage;

public class when_continuing_near_the_storage_ceiling : given.a_page_query
{
    EventSequencePage _lastPage;

    void Establish()
    {
        _window = [EventAt(long.MaxValue - 1), EventAt(long.MaxValue)];
        _cursor.MoveNext().Returns(true, true, false);
    }

    async Task Because()
    {
        _page = await Query(from: long.MaxValue - 1, count: 1);
        _window = [EventAt(long.MaxValue)];
        _lastPage = await Query(from: _page.Next, count: 1);
    }

    [Fact] void should_return_the_event_before_the_ceiling() => _page.Events.Single().Id.ShouldEqual("9223372036854775806");
    [Fact] void should_continue_at_the_ceiling_without_wrapping() => _page.Next.ShouldEqual((ulong?)long.MaxValue);
    [Fact] void should_return_the_event_at_the_ceiling() => _lastPage.Events.Single().Id.ShouldEqual("9223372036854775807");
    [Fact] void should_stop_without_a_lookahead() => _lastPage.Next.ShouldBeNull();
}
