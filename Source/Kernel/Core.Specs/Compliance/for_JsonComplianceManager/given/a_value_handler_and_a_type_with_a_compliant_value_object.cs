// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager.given;

public class a_value_handler_and_a_type_with_a_compliant_value_object : Specification
{
    protected const string Identifier = "u-100-000-000";

    protected JsonSchema _schema;
    protected IJsonCompliancePropertyValueHandler _valueHandler;
    protected JsonComplianceManager _manager;

    protected readonly ComplianceMetadataType _metadataType = "test-metadata-type";

    async Task Establish()
    {
        // Compliance metadata on the value object itself rather than on its leaves. The schema generator no
        // longer emits this shape, but a schema stored before that change still carries it, so the manager must
        // treat the whole object as one value on both sides of the round trip.
        _schema = await JsonSchema.FromJsonAsync(
            """
            {
              "type": "object",
              "properties": {
                "id": { "type": "string" },
                "dateOfBirth": {
                  "type": "object",
                  "properties": {
                    "dateOfBirth": { "type": "string" },
                    "verifiedBy": { "type": "string" }
                  },
                  "compliance": [ { "metadataType": "test-metadata-type", "details": "" } ]
                }
              }
            }
            """);

        _valueHandler = Substitute.For<IJsonCompliancePropertyValueHandler>();
        _valueHandler.Type.Returns(_metadataType);
        _valueHandler.Apply(string.Empty, string.Empty, Identifier, Arg.Any<JsonNode>())
            .Returns(callInfo => Task.FromResult<JsonNode>(JsonValue.Create(Convert.ToBase64String(Encoding.UTF8.GetBytes(callInfo.ArgAt<JsonNode>(3).ToJsonString())))));
        _valueHandler.Release(string.Empty, string.Empty, Identifier, Arg.Any<JsonNode>())
            .Returns(callInfo => Task.FromResult<JsonNode>(JsonValue.Create(Encoding.UTF8.GetString(Convert.FromBase64String(callInfo.ArgAt<JsonNode>(3).GetValue<string>())))));

        _manager = new(new KnownInstancesOf<IJsonCompliancePropertyValueHandler>(_valueHandler), NullLogger<JsonComplianceManager>.Instance);
    }
}
