// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.for_Storage;

public class when_listing_event_stores_with_legacy_names : given.a_storage_with_legacy_event_store_names
{
    IEnumerable<EventStoreName> _result;

    async Task Because() => _result = await _storage.GetEventStores();

    [Fact] void should_only_list_valid_names() => _result.ShouldEqual(new EventStoreName[] { "store-a", "store-b" });
    [Fact] async Task should_not_report_not_set_as_existing() => (await _storage.HasEventStore(EventStoreName.NotSet)).ShouldBeFalse();
    [Fact] async Task should_not_report_a_blank_name_as_existing() => (await _storage.HasEventStore(" ")).ShouldBeFalse();
    [Fact] async Task should_report_a_valid_name_as_existing() => (await _storage.HasEventStore("store-a")).ShouldBeTrue();
}
