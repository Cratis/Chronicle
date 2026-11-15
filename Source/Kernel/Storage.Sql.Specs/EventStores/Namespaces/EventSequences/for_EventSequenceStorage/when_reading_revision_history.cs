// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage;

public class when_reading_revision_history : given.an_event_with_revision_history
{
    AppendedEvent _page;
    AppendedEvent _window;
    AppendedEvent _range;
    AppendedEvent _from;
    AppendedEvent _currentRange;
    AppendedEvent _currentFrom;

    async Task Because()
    {
        await Revise();
        _page = await Read(await _storage.GetPage(new EventSequenceQueryCriteria(), 0, 1));
        _window = await Read(await _storage.GetWindow(EventSequenceNumber.First, EventSequenceNumber.First, 1, false));
        _range = await Read(await _storage.GetRangeWithHistory(EventSequenceNumber.First, EventSequenceNumber.First));
        _from = await Read(await _storage.GetFromSequenceNumberWithHistory(EventSequenceNumber.First));
        _currentRange = await Read(await _storage.GetRange(EventSequenceNumber.First, EventSequenceNumber.First));
        _currentFrom = await Read(await _storage.GetFromSequenceNumber(EventSequenceNumber.First));
    }

    [Fact] void should_return_the_original_on_the_page() => _page.OriginalContent.ShouldContain("original");
    [Fact] void should_return_the_revision_on_the_page() => _page.Revisions.Single().Content.ShouldContain("recorded");
    [Fact] void should_return_the_original_in_the_window() => _window.OriginalContent.ShouldContain("original");
    [Fact] void should_return_the_revision_in_the_window() => _window.Revisions.Single().Content.ShouldContain("recorded");
    [Fact] void should_return_the_original_in_the_range() => _range.OriginalContent.ShouldContain("original");
    [Fact] void should_return_the_revision_in_the_range() => _range.Revisions.Single().Content.ShouldContain("recorded");
    [Fact] void should_return_the_original_from_the_sequence_number() => _from.OriginalContent.ShouldContain("original");
    [Fact] void should_return_the_revision_from_the_sequence_number() => _from.Revisions.Single().Content.ShouldContain("recorded");
    [Fact] void should_omit_the_original_from_the_current_only_range() => _currentRange.OriginalContent.ShouldEqual(string.Empty);
    [Fact] void should_omit_revisions_from_the_current_only_range() => _currentRange.Revisions.ShouldBeEmpty();
    [Fact] void should_omit_the_original_from_the_current_only_sequence_read() => _currentFrom.OriginalContent.ShouldEqual(string.Empty);
    [Fact] void should_omit_revisions_from_the_current_only_sequence_read() => _currentFrom.Revisions.ShouldBeEmpty();
    [Fact] void should_keep_the_current_content_in_the_current_only_range() => ((string)((IDictionary<string, object?>)_currentRange.Content)["value"]!).ShouldEqual("recorded");
    [Fact] void should_keep_the_current_content_in_the_current_only_sequence_read() => ((string)((IDictionary<string, object?>)_currentFrom.Content)["value"]!).ShouldEqual("recorded");

    static async Task<AppendedEvent> Read(IEventCursor cursor)
    {
        using (cursor)
        {
            (await cursor.MoveNext()).ShouldBeTrue();
            return cursor.Current.Single();
        }
    }
}
