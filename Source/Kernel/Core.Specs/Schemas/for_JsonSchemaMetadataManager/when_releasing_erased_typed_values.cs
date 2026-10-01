// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Json;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager;

public class when_releasing_erased_typed_values : given.a_value_handler_and_a_type_with_one_property
{
    JsonObject _result;
    ReleasedAddress _address;
    ExpandoObject _instance;

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync(
            """
            {
              "type": "object",
              "properties": {
                "address": {
                  "type": "object",
                  "properties": {
                    "Street": { "type": "string", "compliance": [{ "metadataType": "test-metadata-type", "details": "" }] },
                    "PostalCode": { "type": "integer", "format": "int32", "compliance": [{ "metadataType": "test-metadata-type", "details": "" }] },
                    "Verified": { "type": "boolean", "compliance": [{ "metadataType": "test-metadata-type", "details": "" }] },
                    "RegionId": { "type": "string", "format": "guid", "compliance": [{ "metadataType": "test-metadata-type", "details": "" }] },
                    "MovedInAt": { "type": "string", "format": "date-time", "compliance": [{ "metadataType": "test-metadata-type", "details": "" }] },
                    "Kind": { "type": "integer", "enum": [0, 1], "compliance": [{ "metadataType": "test-metadata-type", "details": "" }] },
                    "Age": { "type": ["integer", "null"], "compliance": [{ "metadataType": "test-metadata-type", "details": "" }] },
                    "BirthDate": { "type": "string", "format": "date-time?", "compliance": [{ "metadataType": "test-metadata-type", "details": "" }] }
                  }
                },
                "numbers": {
                  "type": "array",
                  "items": { "type": "integer", "compliance": [{ "metadataType": "test-metadata-type", "details": "" }] }
                },
                "optionalNumbers": {
                  "type": "array",
                  "items": { "type": ["integer", "null"], "format": "int32?", "compliance": [{ "metadataType": "test-metadata-type", "details": "" }] }
                },
                "status": { "type": "string" }
              }
            }
            """);
        _input = JsonNode.Parse(
            """
            { "address": { "Street": "ciphertext", "PostalCode": "ciphertext", "Verified": "ciphertext", "RegionId": "ciphertext", "MovedInAt": "ciphertext", "Kind": "ciphertext", "Age": "ciphertext", "BirthDate": "ciphertext" }, "numbers": ["ciphertext"], "optionalNumbers": ["ciphertext"], "status": "active" }
            """)!.AsObject();
        _valueHandler.ReleaseWithStatus(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<string>(), Arg.Any<JsonNode>())
            .Returns(_ => new ReleasedSchemaMetadataValue(JsonValue.Create(string.Empty), IsUnreadable: true));
    }

    async Task Because()
    {
        _result = await _manager.Release(EventStoreName.NotSet, EventStoreNamespaceName.Default, _schema, "erased-subject", _input);
        _address = _result["address"]!.Deserialize<ReleasedAddress>()!;
        _instance = new ExpandoObjectConverter(new TypeFormats()).ToExpandoObject(_result, _schema);
    }

    [Fact] void should_release_the_street_as_empty() => _address.Street.ShouldEqual(string.Empty);
    [Fact] void should_release_the_number_as_zero() => _address.PostalCode.ShouldEqual(0);
    [Fact] void should_release_the_flag_as_false() => _address.Verified.ShouldBeFalse();
    [Fact] void should_release_the_identifier_as_empty() => _address.RegionId.ShouldEqual(Guid.Empty);
    [Fact] void should_release_the_date_as_default() => _address.MovedInAt.ShouldEqual(default);
    [Fact] void should_release_the_enum_as_zero() => _address.Kind.ShouldEqual(0);
    [Fact] void should_release_the_optional_number_as_null() => _address.Age.ShouldBeNull();
    [Fact] void should_release_the_optional_date_as_null() => _address.BirthDate.ShouldBeNull();
    [Fact] void should_release_collection_elements_as_their_declared_type() => _result["numbers"]!.Deserialize<int[]>()!.ShouldContainOnly(0);
    [Fact] void should_release_optional_collection_elements_as_null() => _result["optionalNumbers"]!.Deserialize<int?[]>()![0].ShouldBeNull();
    [Fact] void should_materialize_optional_collection_elements_as_null() => ((object?[])((IDictionary<string, object?>)_instance)["optionalNumbers"]!)[0].ShouldBeNull();
    [Fact] void should_preserve_the_non_personal_value() => _result["status"]!.GetValue<string>().ShouldEqual("active");
    [Fact] void should_not_change_the_input() => _input["address"]!["PostalCode"]!.GetValue<string>().ShouldEqual("ciphertext");

    record ReleasedAddress(string Street, int PostalCode, bool Verified, Guid RegionId, DateTime MovedInAt, int Kind, int? Age, DateTime? BirthDate);
}
