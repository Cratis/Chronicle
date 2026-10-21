// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeGenerationBackfill.when_performing;

public class and_the_source_generation_is_not_in_the_definition : given.a_backfill
{
    Exception _error;

    void Establish()
    {
        _snapshot = _snapshot with
        {
            AppendedGeneration = 9,
            Content = new Dictionary<EventTypeGeneration, string> { [9] = """{"value":"from the future"}""" }
        };
    }

    async Task Because() => _error = await Catch.Exception(Perform);

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_not_write() => _writes.ShouldBeEmpty();
}
