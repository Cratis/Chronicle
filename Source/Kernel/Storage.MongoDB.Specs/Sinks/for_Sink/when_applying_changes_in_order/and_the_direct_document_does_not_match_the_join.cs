// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_in_order;

public class and_the_direct_document_does_not_match_the_join : given.a_sink_recording_writes
{
    void Establish()
    {
        _key = "direct-only";
        _changes = [Direct(10), Join(20)];
    }

    async Task Because() => await Apply();

    [Fact] void should_store_the_direct_value() => _documents["direct-only"]["value"].AsInt32.ShouldEqual(10);
    [Fact] void should_store_the_joined_value_on_matching_documents() => _documents["joined-only"]["value"].AsInt32.ShouldEqual(20);
}
