// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventCompliance.when_releasing_history;

public class and_revision_json_is_not_an_object : given.an_event_with_protected_history
{
    void Establish() => _event = _event with
    {
        Revisions = new[] { "[]", "null", "42", "\"text\"", "true", "", "{" }
            .Select(content => _event.Revisions.Single() with { Content = content }).ToArray()
    };

    async Task Because() => _result = await _compliance.Release([_event], _schemas);

    [Fact] void should_preserve_non_object_revisions() => _result[0].Revisions.ShouldEqual(_event.Revisions);
    [Fact] void should_still_release_object_history() => Value(_result[0].GenerationalContent[1], 1).ShouldEqual("released");
}
