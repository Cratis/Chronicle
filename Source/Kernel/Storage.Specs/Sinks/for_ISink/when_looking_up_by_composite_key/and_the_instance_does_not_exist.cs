// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks.for_ISink.when_looking_up_by_composite_key;

public abstract class and_the_instance_does_not_exist<THarness> : given.a_composite_key_read_model<THarness>
    where THarness : ISinkHarness, new()
{
    CompositeKeyLookupResult _result;

    async Task Because() => _result = await Lookup(CompositeKey("missing", "key"));

    [Fact] public void should_not_find_the_instance() => _result.Outcome.ShouldEqual(CompositeKeyLookupOutcome.NotFound);
    [Fact] public void should_not_return_an_instance() => _result.Instance.ShouldBeNull();
}
