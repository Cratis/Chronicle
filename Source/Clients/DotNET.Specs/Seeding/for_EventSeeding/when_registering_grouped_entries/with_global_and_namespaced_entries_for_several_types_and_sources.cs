// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_registering_grouped_entries;

public class with_global_and_namespaced_entries_for_several_types_and_sources : given.a_seeding_builder_that_records_serialization
{
    void Establish()
    {
        _seeding.ForEventSource("s1", [new Alpha("a1"), new Alpha("a2")]);
        _seeding.ForEventSource("s1", [new Beta("b1")]);
        _seeding.ForEventSource("s2", [new Alpha("a3")]);
        _seeding.ForNamespace("ns-one").ForEventSource("s1", [new Alpha("n1a")]);
        _seeding.ForNamespace("ns-two").ForEventSource("s3", [new Beta("n2b")]);
        _seeding.ForNamespace("ns-one").ForEventSource("s4", [new Beta("n1b")]);
    }

    async Task Because() => await _seeding.Register();

    [Fact] void should_send_a_single_request() => _requests.Count.ShouldEqual(1);
    [Fact] void should_not_be_authoritative_for_the_global_set() => _requests[0].GlobalSetIsAuthoritative.ShouldBeFalse();
    [Fact] void should_group_global_entries_by_event_type_in_first_seen_order() => _requests[0].GlobalByEventType.Select(_ => _.EventTypeId).ToArray().ShouldEqual(["alpha", "beta"]);
    [Fact] void should_keep_global_entries_for_an_event_type_in_registration_order() => _requests[0].GlobalByEventType.ElementAt(0).Entries.Select(_ => _.EventSourceId).ToArray().ShouldEqual(["s1", "s1", "s2"]);
    [Fact] void should_group_global_entries_by_event_source_in_first_seen_order() => _requests[0].GlobalByEventSource.Select(_ => _.EventSourceId).ToArray().ShouldEqual(["s1", "s2"]);
    [Fact] void should_keep_global_entries_for_an_event_source_in_registration_order() => _requests[0].GlobalByEventSource.ElementAt(0).Entries.Select(_ => _.EventTypeId).ToArray().ShouldEqual(["alpha", "alpha", "beta"]);
    [Fact] void should_serialize_the_event_content() => _requests[0].GlobalByEventSource.ElementAt(0).Entries.Select(_ => _.Content).ToArray().ShouldEqual(["{\"value\":\"a1\"}", "{\"value\":\"a2\"}", "{\"value\":\"b1\"}"]);
    [Fact] void should_group_namespaces_in_first_seen_order() => _requests[0].NamespacedEntries.Select(_ => _.Namespace).ToArray().ShouldEqual(["ns-one", "ns-two"]);
    [Fact] void should_merge_entries_added_to_the_same_namespace_separately() => _requests[0].NamespacedEntries.ElementAt(0).ByEventSource.Select(_ => _.EventSourceId).ToArray().ShouldEqual(["s1", "s4"]);
    [Fact] void should_group_namespaced_entries_by_event_type() => _requests[0].NamespacedEntries.ElementAt(0).ByEventType.Select(_ => _.EventTypeId).ToArray().ShouldEqual(["alpha", "beta"]);
    [Fact] void should_serialize_every_entry_exactly_once_in_namespace_group_order() => _serialized.ShouldEqual(["a1", "a2", "b1", "a3", "n1a", "n1b", "n2b"]);
    [Fact] void should_share_the_same_contract_entry_between_both_groupings() => _requests[0].GlobalByEventType.ElementAt(0).Entries[2].ShouldBeSame(_requests[0].GlobalByEventSource.ElementAt(1).Entries[0]);
}
