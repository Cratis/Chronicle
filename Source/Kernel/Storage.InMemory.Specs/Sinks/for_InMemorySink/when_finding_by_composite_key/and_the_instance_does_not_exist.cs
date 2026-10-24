// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.Sinks.for_InMemorySink.when_finding_by_composite_key;

public class and_the_instance_does_not_exist : given.a_sink_with_composite_keys
{
    IDictionary<string, object?>? _result;

    async Task Establish() => await _sink.ApplyChanges(CompositeKey(("a", "x"), ("b", "y")), ChangesetSettingAmount(1), 1UL);

    async Task Because() => _result = await _sink.FindOrDefault(CompositeKey(("a", "x"), ("b", "other")));

    [Fact] void should_not_find_anything() => _result.ShouldBeNull();
}
