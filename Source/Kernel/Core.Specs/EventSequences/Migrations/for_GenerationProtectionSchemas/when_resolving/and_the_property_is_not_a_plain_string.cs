// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.EventSequences.Migrations.for_GenerationProtectionSchemas.when_resolving;

public class and_the_property_is_not_a_plain_string : Specification
{
    JsonSchema _own;
    JsonSchema _higher;
    JsonSchema _effective;

    void Establish()
    {
        _own = JsonSchema.FromJson("""{"type":"object","properties":{"number":{"type":"integer"},"date":{"type":"string","format":"date-time"}}}""");
        _higher = JsonSchema.FromJson("""{"type":"object","properties":{"number":{"type":"integer","compliance":[{"metadataType":"PII","details":""}]},"date":{"type":"string","format":"date-time","compliance":[{"metadataType":"PII","details":""}]}}}""");
    }

    void Because() => _effective = GenerationProtectionSchemas.For(1, [((EventTypeGeneration)1, _own), ((EventTypeGeneration)2, _higher)]);

    [Fact] void should_not_inherit_metadata() => _effective.HasSchemaMetadata().ShouldBeFalse();
    [Fact] void should_return_the_registered_instance() => _effective.ShouldBeSame(_own);
}
