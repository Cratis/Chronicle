// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Setup.Serialization.for_AppendedEventSerializer.when_serializing;

public class and_appended_generation_and_hashes_round_trip : given.a_serializer_for_appended_events
{
    AppendedEvent _event;
    AppendedEvent _result;
    void Establish()
    {
        SchemaLookupReturns();
        _event = AnEvent() with
        {
            Context = AnEvent().Context with { AppendedGeneration = 2 },
            GenerationalHashes = new Dictionary<int, EventHash> { [1] = "first-hash", [2] = "second-hash" },
            RevisedGeneration = 1
        };
    }
    void Because() => _result = _serializer.Deserialize<AppendedEvent>(Serialize(_event));
    [Fact] void should_keep_the_appended_generation() => _result.Context.AppendedGeneration!.Value.ShouldEqual(2U);
    [Fact] void should_keep_the_generation_hashes() => _result.GenerationalHashes[2].Value.ShouldEqual("second-hash");
    [Fact] void should_keep_the_revision_generation() => _result.RevisedGeneration!.Value.ShouldEqual(1U);
}
