// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Diagnostics.Identifiers.for_IdentifierSpanProcessor;

public class when_hashing_identifiers : Specification
{
    const string Key = "chronicle-identifier-hash-key-for-specs-0001";
    Activity _activity;
    IdentifierSpanProcessor _processor;

    void Establish()
    {
        _activity = new Activity("spec")
            .SetTag("orleans.rpc.target_id", "observer-subscriber/abc")
            .SetTag("orleans.rpc.source_id", "observer-subscriber/def/ghi")
            .SetTag("cratis.eventsource.id", "8ce3c6dbf2036756ec5ef13d72ec10c6");
        _processor = new IdentifierSpanProcessor(Options.Create(new ChronicleOptions
        {
            Telemetry = new() { EventSourceIdsInSpans = IdentifierExposure.Hashed, IdentifierHashKey = Key }
        }));
    }

    void Because() => _processor.OnEnd(_activity);

    [Fact] void should_preserve_the_target_type_and_hash_its_key() => _activity.GetTagItem("orleans.rpc.target_id").ShouldEqual($"observer-subscriber/{new IdentifierHasher(Key).Hash("abc")}");
    [Fact] void should_split_the_source_id_at_only_the_first_slash() => _activity.GetTagItem("orleans.rpc.source_id").ShouldEqual($"observer-subscriber/{new IdentifierHasher(Key).Hash("def/ghi")}");
    [Fact] void should_preserve_the_already_hashed_event_source_id() => _activity.GetTagItem("cratis.eventsource.id").ShouldEqual("8ce3c6dbf2036756ec5ef13d72ec10c6");

    void Destroy() => _activity.Dispose();
}
