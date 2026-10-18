// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_the_content_carries_metadata_without_a_handler : given.an_append_intent
{
    JsonSchemaMetadataManager _metadata;
    JsonSchema _schema;
    async Task Establish()
    {
        _metadata = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(), NullLogger<JsonSchemaMetadataManager>.Instance);
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"unknown","details":""}]},"count":{"type":"integer"}}}""");
    }
    async Task Because()
    {
        var locations = await AppendFingerprint.LocateProtectedValues(_metadata, _schema, "source", _input.Content);
        locations.TryGetResult(out var values).ShouldBeTrue();
        _input = _input with { ProtectedValues = values };
        _input.Content["name"] = "Bob";
        _result = Compute(_input);
    }
    [Fact] void should_cover_the_value_as_plain_content() => _result.Plain.ShouldNotEqual(_original.Plain);
    [Fact] void should_not_create_a_protected_part() => _result.ProtectedParts.ShouldBeEmpty();
}
