// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Chronicle.Confidentiality;
using Cratis.Chronicle.ProtectedValues;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_read_model_with_nullable_concept_dictionary_values : given.a_json_schema_generator_with_protection_support
{
    [PII("Login details")]
    record Login(string Value) : ConceptAs<string>(Value);

    [Encrypted]
    record Identifier(Guid Value) : ConceptAs<Guid>(Value);

    record State(
        Dictionary<Guid, Login?> OptionalDictionary,
        IDictionary<Guid, Login?> OptionalInterface,
        IReadOnlyDictionary<Guid, Login?> OptionalReadOnlyInterface,
        ReadOnlyDictionary<Guid, Login?> OptionalReadOnly,
        ConcurrentDictionary<Guid, Login?> OptionalConcurrent,
        SortedDictionary<Guid, Login?> OptionalSorted,
        ImmutableDictionary<Guid, Login?> OptionalImmutable,
        Dictionary<Guid, Identifier?> Formatted,
        Dictionary<Guid, Login> Required,
        Dictionary<Guid, Login>? OptionalContainer,
        Dictionary<Guid, int> Numbers,
        List<Login> Logins)
    {
        public Dictionary<Guid, Login?> Property { get; init; } = [];
    }

    JsonSchema _result;

    void Because() => _result = _generator.GenerateForReadModel(typeof(State));

    [Fact] void should_preserve_nullable_unformatted_values() => OptionalValues().Select(_ => _.Type).ShouldEqual(Enumerable.Repeat(JsonObjectType.String | JsonObjectType.Null, 7));
    [Fact] void should_preserve_nullable_value_metadata() => OptionalValues().Select(_ => _.GetComplianceMetadata().Single()).ShouldEqual(Enumerable.Repeat(new ComplianceSchemaMetadata(ComplianceMetadataType.PII.Value, "Login details"), 7));
    [Fact] void should_preserve_nullable_formatted_values() => ValueSchema("Formatted").Format.ShouldEqual("guid?");
    [Fact] void should_preserve_encrypted_value_metadata() => ValueSchema("Formatted").GetSecurityMetadata().Single().ShouldEqual(new ComplianceSchemaMetadata(SecurityMetadataType.EncryptedSubject.Value, ""));
    [Fact] void should_preserve_property_value_nullability() => ValueSchema("Property").Type.ShouldEqual(JsonObjectType.String | JsonObjectType.Null);
    [Fact] void should_keep_required_values_non_nullable() => ValueSchema("Required").Type.ShouldEqual(JsonObjectType.String);
    [Fact] void should_not_use_container_nullability_for_values() => ValueSchema("OptionalContainer").Type.ShouldEqual(JsonObjectType.String);
    [Fact] void should_keep_non_concept_values_typed() => ValueSchema("Numbers").Type.ShouldEqual(JsonObjectType.Integer);
    [Fact] void should_not_treat_a_list_as_a_dictionary() => _result.ActualProperties["Logins"].Type.ShouldEqual(JsonObjectType.Array);
    [Fact] void should_keep_list_items_typed() => _result.ActualProperties["Logins"].Item.Type.ShouldEqual(JsonObjectType.String);

    JsonSchema ValueSchema(string name) => _result.ActualProperties[name].AdditionalPropertiesSchema!;

    IEnumerable<JsonSchema> OptionalValues() => _result.ActualProperties
        .Where(_ => _.Key.StartsWith("Optional", StringComparison.Ordinal) && _.Key != "OptionalContainer")
        .Select(_ => _.Value.AdditionalPropertiesSchema!);
}
