// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventConverter.when_converting_event_to_appended_event;

public class and_generation_metadata_is_stored : given.an_event_converter
{
    Event _event;
    AppendedEvent _result;
    void Establish() => _event = CreateEvent() with { AppendedGeneration = 1 };
    async Task Because() => _result = await _converter.ToAppendedEvent(_event);
    [Fact] void should_expose_the_appended_generation() => _result.Context.AppendedGeneration!.Value.ShouldEqual(1U);
    [Fact] void should_expose_the_hashes() => _result.GenerationalHashes[1].Value.ShouldEqual("hash");
}
