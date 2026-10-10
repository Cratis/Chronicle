// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.when_releasing;

public class and_sql_records_a_revision_below_the_context_generation : given.a_release_boundary
{
    void Establish() => _event = _event with
    {
        Context = _event.Context with { EventType = _pin },
        RevisedGeneration = 1,
        GenerationalContent = new Dictionary<int, string> { [1] = "{\"name\":\"revised-name\"}", [2] = "{\"fullName\":\"old-name\"}" }
    };

    async Task Because() => _result = await _release.Release(_event.Context.EventStore, [_pin], _schemas, [_event]);

    [Fact] void should_migrate_the_revision_instead_of_using_the_context_generation() => ((IDictionary<string, object?>)_result[0].Content)["fullName"].ShouldEqual("revised-name");
}
