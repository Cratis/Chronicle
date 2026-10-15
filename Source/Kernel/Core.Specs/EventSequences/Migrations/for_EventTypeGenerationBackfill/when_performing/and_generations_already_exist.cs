// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.Migrations.for_EventTypeGenerationBackfill.when_performing;

public class and_generations_already_exist : given.a_backfill
{
    void Establish()
    {
        _snapshot = _snapshot with { Content = _snapshot.Content.Append(new(new EventTypeGeneration(3), "{}" )).ToDictionary() };
    }

    async Task Because() => await Perform();

    [Fact] void should_not_write() => _writes.ShouldBeEmpty();
}
