// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_querying_with_named_tags;

public class and_paging : given.a_storage_with_named_tags
{
    IEnumerable<AppendedEvent> _events;

    async Task Because()
    {
        using var page = await _storage.GetPage(new() { NamedTags = [new(new TagName("account"))] }, 1, 1);
        await page.MoveNext();
        _events = page.Current.ToArray();
    }

    [Fact] void should_page_only_matching_events() => _events.Single().Context.SequenceNumber.Value.ShouldEqual(1UL);
    [Fact] void should_hydrate_named_tags() => _events.Single().Context.NamedTags.Select(_ => _.Value).ShouldEqual(["b", "99"]);
}
