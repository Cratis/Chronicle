// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Imports;

namespace Cratis.Chronicle.Storage.Imports.for_ImportStorageSerialization;

public class when_serializing_a_chunk_with_json_content : Specification
{
    const string Content = """{"name":"Alice","quote":"<b>\"hi\"</b> & 'bye'","note":"æøå"}""";
    StagedImportChunk _chunk;
    string _json;
    StagedImportChunk _read;

    void Establish() => _chunk = new(
        ImportId.New(),
        0,
        0,
        1,
        [new StagedImportEvent(0, "first+1", "Default", "source", "All", "Default", Content, [], [], null, null, null, null, null, null, null, null)],
        []);

    void Because()
    {
        _json = ImportStorageSerialization.Serialize(_chunk);
        _read = ImportStorageSerialization.DeserializeChunk(_json);
    }

    [Fact] void should_not_escape_quotes_as_unicode_sequences() => _json.ShouldNotContain("\\u0022");
    [Fact] void should_not_escape_markup_or_non_ascii_characters() => (_json.Contains("\\u003C") || _json.Contains("\\u0026") || _json.Contains("\\u00E6")).ShouldBeFalse();
    [Fact] void should_round_trip_the_content_exactly() => _read.Events.Single().Content.ShouldEqual(Content);
}
