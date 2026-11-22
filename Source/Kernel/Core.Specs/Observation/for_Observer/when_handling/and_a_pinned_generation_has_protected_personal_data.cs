// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_a_pinned_generation_has_protected_personal_data : given.an_observer_releasing_pinned_history
{
    async Task Because() => await HandleEvent();

    [Fact] void should_deliver_released_personal_data() => ((IDictionary<string, object?>)_delivered.Content)["fullName"].ShouldEqual("Ada Lovelace");
    [Fact] void should_deliver_the_pinned_generation() => _delivered.Context.EventType.ShouldEqual(_pin);
    [Fact] void should_deliver_the_pinned_hash() => _delivered.Context.Hash.ShouldEqual((EventHash)"pinned-hash");
    [Fact] void should_preserve_the_appended_generation() => _delivered.Context.AppendedGeneration!.Value.ShouldEqual(1U);
    [Fact] void should_release_generational_history() => _delivered.GenerationalContent.Values.ShouldEachConformTo(content => content.Contains("Ada Lovelace", StringComparison.Ordinal));
    [Fact] void should_release_original_history() => Value(_delivered.OriginalContent, "name").ShouldEqual("Ada Lovelace");
    [Fact] void should_leave_storage_content_protected() => _appended.GenerationalContent[2].ShouldEqual("{\"fullName\":\"ciphertext\"}");
}
