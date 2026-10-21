// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.EventSequences.Idempotency.for_AppendFingerprint;

public class when_a_boundary_carries_two_protections : given.an_append_intent
{
    JsonSchemaMetadataManager _metadata;
    JsonSchema _schema;
    FingerprintFailure _failure;
    async Task Establish()
    {
        var first = Substitute.For<IJsonSchemaMetadataValueHandler>();
        first.Category.Returns(SchemaMetadataCategory.Compliance);
        first.Type.Returns((SchemaMetadataTypeName)"first");
        var second = Substitute.For<IJsonSchemaMetadataValueHandler>();
        second.Category.Returns(SchemaMetadataCategory.Compliance);
        second.Type.Returns((SchemaMetadataTypeName)"second");
        _metadata = new(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(first, second), NullLogger<JsonSchemaMetadataManager>.Instance);
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"name":{"type":"string","compliance":[{"metadataType":"first","details":""},{"metadataType":"second","details":""}]},"count":{"type":"integer"}}}""");
    }
    async Task Because()
    {
        var result = await AppendFingerprint.LocateProtectedValues(_metadata, _schema, "source", _input.Content);
        result.TryGetError(out _failure).ShouldBeTrue();
    }
    [Fact] void should_map_the_boundary_refusal() => _failure.ShouldEqual(FingerprintFailure.FingerprintUnsupported);
}
