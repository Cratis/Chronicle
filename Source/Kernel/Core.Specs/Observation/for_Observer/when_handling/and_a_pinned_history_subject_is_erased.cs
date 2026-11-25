// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_a_pinned_history_subject_is_erased : given.an_observer_releasing_pinned_history
{
    void Establish()
    {
        _releasedValue = string.Empty;
        _includeRevision = true;
    }

    async Task Because() => await HandleEvent();

    [Fact] void should_empty_current_content() => ((IDictionary<string, object?>)_delivered.Content)["fullName"].ShouldEqual(string.Empty);
    [Fact] void should_empty_generational_history() => _delivered.GenerationalContent.ShouldEachConformTo(item => Value(item.Value, item.Key == 1 ? "name" : "fullName").Length == 0);
    [Fact] void should_empty_original_history() => Value(_delivered.OriginalContent, "name").ShouldEqual(string.Empty);
    [Fact] void should_empty_revision_history() => Value(_delivered.Revisions.Single().Content, "fullName").ShouldEqual(string.Empty);
}
