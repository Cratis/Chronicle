// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

/// <summary>
/// A type bringing its own <see cref="JsonConverter"/> serializes as something other than its CLR shape, and
/// System.Text.Json's schema exporter cannot see through the converter. Adorning the type with
/// <see cref="JsonSchemaTypeAttribute"/> declares what the converter actually produces, so the schema — which is
/// what the value is stored and read against — describes the wire form rather than the CLR form.
/// </summary>
public class when_generating_schema_for_type_with_json_schema_type_override : given.a_json_schema_generator
{
    [JsonSchemaType(typeof(Guid))]
    [JsonConverter(typeof(TrackingCodeJsonConverter))]
    record TrackingCode(Guid Value)
    {
        public string Display => Value.ToString("N");
    }

    class TrackingCodeJsonConverter : JsonConverter<TrackingCode>
    {
        public override TrackingCode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new(reader.GetGuid());

        public override void Write(Utf8JsonWriter writer, TrackingCode value, JsonSerializerOptions options) => writer.WriteStringValue(value.Value);
    }

    record Shipment(TrackingCode Code, TrackingCode? PreviousCode);

    JsonSchema _result;

    void Because() => _result = _generator.Generate(typeof(Shipment));

    [Fact] void should_represent_the_adorned_type_with_the_declared_types_format() => _result.ActualProperties[nameof(Shipment.Code)].Format.ShouldEqual("guid");
    [Fact] void should_represent_the_adorned_type_with_the_declared_types_json_type() => _result.ActualProperties[nameof(Shipment.Code)].Type.ShouldEqual(JsonObjectType.String);
    [Fact] void should_not_describe_the_clr_shape_of_the_adorned_type() => _result.ActualProperties[nameof(Shipment.Code)].ActualProperties.ShouldBeEmpty();
    [Fact] void should_mark_the_optional_adorned_type_as_nullable() => _result.ActualProperties[nameof(Shipment.PreviousCode)].Format.ShouldEqual("guid?");
    [Fact] void should_default_the_optional_adorned_type_to_null() => _result.ActualProperties[nameof(Shipment.PreviousCode)].GetDefaultValue(_typeFormats).ShouldBeNull();
    [Fact] void should_default_the_required_adorned_type_to_its_type_default() => _result.ActualProperties[nameof(Shipment.Code)].GetDefaultValue(_typeFormats).ShouldNotBeNull();
}
