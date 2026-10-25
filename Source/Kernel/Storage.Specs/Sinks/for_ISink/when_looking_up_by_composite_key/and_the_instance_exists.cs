// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_looking_up_by_composite_key;

public abstract class and_the_instance_exists<THarness> : given.a_composite_key_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    CompositeKeyLookupResult _result;
    IDictionary<string, object?>? _key;

    async Task Establish() => await _sink.ApplyChanges(CompositeKey("a", "b"), ChangesetSettingCountTo(42), 42UL);

    async Task Because()
    {
        _result = await Lookup(CompositeKey("a", "b"));
        _key = ((IDictionary<string, object?>)_result.Instance!)["id"] as IDictionary<string, object?>;
    }

    [Fact] public void should_find_the_instance() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.Found);
    [Fact] public void should_return_the_key_as_an_object() => _key.ShouldNotBeNull();
    [Fact] public void should_return_the_first_key_part() => _key!["first"].ShouldEqual("a");
    [Fact] public void should_return_the_second_key_part() => _key!["second"].ShouldEqual("b");
}
