// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventCompliance.when_releasing_history;

public class and_the_subject_is_erased : given.an_event_with_protected_history
{
    void Establish() => _releasedValue = string.Empty;

    async Task Because() => _result = await _compliance.Release([_event], _schemas);

    [Fact] void should_empty_every_protected_generation() => _result[0].GenerationalContent.ShouldEachConformTo(item => Value(item.Value, item.Key).Length == 0);
    [Fact] void should_empty_original_content() => Value(_result[0].OriginalContent, 1).ShouldEqual(string.Empty);
    [Fact] void should_empty_revision_content() => Value(_result[0].Revisions.Single().Content, 2).ShouldEqual(string.Empty);
}
