// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_a_pinned_revision_has_protected_personal_data : given.an_observer_releasing_pinned_history
{
    void Establish() => _includeRevision = true;

    async Task Because() => await HandleEvent();

    [Fact] void should_deliver_released_revision_content() => ((IDictionary<string, object?>)_delivered.Content)["fullName"].ShouldEqual("Ada Lovelace");
    [Fact] void should_deliver_the_pinned_generation() => _delivered.Context.EventType.ShouldEqual(_pin);
    [Fact] void should_not_reuse_the_stale_pinned_hash() => _delivered.Context.Hash.ShouldEqual(EventHash.NotSet);
    [Fact] void should_release_revision_history() => Value(_delivered.Revisions.Single().Content, "fullName").ShouldEqual("Ada Lovelace");
    [Fact] void should_release_original_history() => Value(_delivered.OriginalContent, "name").ShouldEqual("Ada Lovelace");
    [Fact] void should_release_generational_history() => _delivered.GenerationalContent.Values.ShouldEachConformTo(content => content.Contains("Ada Lovelace", StringComparison.Ordinal));
    [Fact] void should_leave_stored_revision_protected() => Value(_appended.Revisions.Single().Content, "fullName").ShouldEqual("ciphertext");
}
