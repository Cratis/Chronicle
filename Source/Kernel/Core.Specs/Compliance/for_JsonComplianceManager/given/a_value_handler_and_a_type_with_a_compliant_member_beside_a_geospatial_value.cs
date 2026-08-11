// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Compliance.for_JsonComplianceManager.given;

/// <summary>
/// A [PII] concept anywhere in a type turns compliance handling on for the whole document, so a geospatial
/// value that carries no personal data at all still gets walked. The schema generator emits Point, LineString
/// and Polygon as leaves carrying only their format - their GeoJSON members belong to the type's converter -
/// while the value on the wire is an object. That combination is the shape under test here.
/// </summary>
public class a_value_handler_and_a_type_with_a_compliant_member_beside_a_geospatial_value : Specification
{
    protected const string DisplayName = "Ada Lovelace";
    protected const string City = "Oslo";
    protected const double Longitude = 10.7522;
    protected const double Latitude = 59.9139;

    protected JsonSchema _schema;
    protected JsonObject _input;
    protected IJsonCompliancePropertyValueHandler _valueHandler;
    protected JsonComplianceManager _manager;

    protected readonly ComplianceMetadataType _metadataType = "test-metadata-type";

    async Task Establish()
    {
        _schema = await JsonSchema.FromJsonAsync(
            """
            {
              "type": "object",
              "properties": {
                "id": { "type": "string" },
                "organizerDisplayName": { "type": "string", "compliance": [ { "metadataType": "test-metadata-type", "details": "" } ] },
                "location": {
                  "type": "object",
                  "properties": {
                    "locationPoint": { "type": "object", "format": "point", "title": "Point" },
                    "city": { "type": "string" }
                  }
                }
              }
            }
            """);

        _input = new JsonObject
        {
            ["id"] = "v-100-000-000",
            ["organizerDisplayName"] = DisplayName,
            ["location"] = new JsonObject
            {
                ["locationPoint"] = new JsonObject
                {
                    ["type"] = "Point",
                    ["coordinates"] = new JsonArray(Longitude, Latitude)
                },
                ["city"] = City
            }
        };

        _valueHandler = Substitute.For<IJsonCompliancePropertyValueHandler>();
        _valueHandler.Type.Returns(_metadataType);
        _manager = new(new KnownInstancesOf<IJsonCompliancePropertyValueHandler>(_valueHandler), NullLogger<JsonComplianceManager>.Instance);
    }
}
