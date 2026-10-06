// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsCompliance;

public class when_fencing_nullable_scalar_arrays : Specification
{
    ReadModelsCompliance _compliance;
    ExpandoObjectConverter _converter;
    JsonSchema _schema;
    ExpandoObject _original;
    ExpandoObject _result;

    async Task Establish()
    {
        var keys = new InMemoryEncryptionKeyStorage();
        await keys.RecordErasureFor("store", "namespace", "owner");
        var encryption = new Encryption();
        var handler = new PIICompliancePropertyValueHandler(new ManagedEncryptionKeyProvisioner(keys, encryption), keys, encryption);
        var manager = new JsonSchemaMetadataManager(new KnownInstancesOf<IJsonSchemaMetadataValueHandler>(handler), NullLogger<JsonSchemaMetadataManager>.Instance);
        _converter = new(new TypeFormats());
        _compliance = new(manager, _converter);
        _schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"numbers":{"type":"array","items":{"type":["integer","null"],"format":"int32?","compliance":[{"metadataType":"PII","details":""}]}}}}""");
        _original = _converter.ToExpandoObject(JsonNode.Parse("""{"numbers":[42,7]}""")!.AsObject(), _schema);
    }

    async Task Because() => _result = await _compliance.ApplyErasureFence("store", "namespace", _schema, "owner", _original);

    [Fact] void should_preserve_erased_element_positions() => ((object?[])((IDictionary<string, object?>)_result)["numbers"]!).ShouldContainOnly(null, null);
    [Fact] void should_round_trip_erased_elements() => JsonNode.DeepEquals(_converter.ToJsonObject(_result, _schema), JsonNode.Parse("""{"numbers":[null,null]}""")).ShouldBeTrue();
    [Fact] void should_not_mutate_the_original_plaintext() => ((object?[])((IDictionary<string, object?>)_original)["numbers"]!).ShouldContainOnly(42, 7);
}
