// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_finding_by_composite_key;

public class and_parts_are_given_in_another_order : given.a_sink_with_composite_keys
{
    IDictionary<string, object?>? _result;

    async Task Establish() => await _sink.ApplyChanges(CompositeKey(("Currency", "EUR"), ("Invoice", "42")), ChangesetSettingAmount(7), 1UL);

    async Task Because() => _result = await _sink.FindOrDefault(CompositeKey(("Invoice", "42"), ("Currency", "EUR")));

    [Fact] void should_find_the_instance() => _result!["amount"].ShouldEqual(7);
}
