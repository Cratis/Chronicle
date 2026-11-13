// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Imports;
using Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Chronicle.Imports.for_EventImport.when_adding;

public class and_the_metadata_exceeds_the_byte_limit : given.an_event_import
{
    void Establish()
    {
        _maxChunkEvents = 100;
        _maxChunkBytes = 250;
        _convert = @event => new ImportEvent
        {
            EventSourceId = @event.EventSourceId,
            Content = "{}",
            EventType = new() { Id = "event", Generation = 1 },
            Tags = [new string('t', 40)],
            Causation = [new Causation { Type = "Legacy", Properties = new Dictionary<string, string> { ["source"] = new string('c', 50) } }],
            IdempotencyKey = new string('k', 10)
        };
    }

    async Task Because()
    {
        await Import.Add(Events(5));
        await Import.Complete();
    }

    [Fact] void should_count_tags_causation_and_keys_toward_the_bytes() => _chunks.OrderBy(_ => _.ChunkIndex).Select(_ => _.Events.Count()).ShouldEqual([2, 2, 1]);
}
