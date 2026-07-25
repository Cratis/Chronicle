// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage;

public class when_replacing_generation_content : given.a_storage_with_appended_events
{
    AppendedEvent _stored;

    async Task Because()
    {
        var secondGeneration = new ExpandoObject();
        ((IDictionary<string, object?>)secondGeneration)["value"] = "migrated";

        await _storage.ReplaceGenerationContent(
            1,
            new Dictionary<EventTypeGeneration, ExpandoObject>
            {
                { EventTypeGeneration.First, new ExpandoObject() },
                { new EventTypeGeneration(2), secondGeneration }
            });

        _stored = _storage.Events.Single(_ => _.Context.SequenceNumber == (EventSequenceNumber)1UL);
    }

    [Fact] void should_expose_the_latest_generation_as_the_content() => ((IDictionary<string, object?>)_stored.Content)["value"].ShouldEqual("migrated");
    [Fact] void should_retain_every_generation() => _stored.GenerationalContent.Keys.Order().ShouldEqual([1, 2]);
}
