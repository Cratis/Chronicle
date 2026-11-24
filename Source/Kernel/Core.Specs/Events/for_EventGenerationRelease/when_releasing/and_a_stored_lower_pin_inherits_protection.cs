// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.when_releasing;

public class and_a_stored_lower_pin_inherits_protection : given.a_downcast_protected_token
{
    async Task Because() => _result = await Release();

    [Fact] void should_deliver_plaintext() => ReleasedToken.ShouldEqual(Token);
    [Fact] void should_return_exactly_the_stored_generations() => _result[0].GenerationalContent.Keys.Order().ToArray().ShouldEqual(_event.GenerationalContent.Keys.Order().ToArray());
    [Fact] void should_have_returned_generations() => _result[0].GenerationalContent.Count.ShouldEqual(3);
    [Fact] void should_release_every_returned_generation() => _result[0].GenerationalContent.Values.Count(content => content.Contains(Token, StringComparison.Ordinal)).ShouldEqual(_result[0].GenerationalContent.Count);
    [Fact] void should_keep_storage_protected() => _event.GenerationalContent[1].ShouldEqual(StoredGeneration(1));
}
