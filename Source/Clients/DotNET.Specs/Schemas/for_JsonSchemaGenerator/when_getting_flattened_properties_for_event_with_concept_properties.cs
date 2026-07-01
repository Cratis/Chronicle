// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_getting_flattened_properties_for_event_with_concept_properties : given.a_json_schema_generator
{
    record EmailValue(string Value) : ConceptAs<string>(Value);
    record ReferenceValue(Guid Value) : ConceptAs<Guid>(Value);
    record AccountRegistered(EmailValue Email, ReferenceValue Reference, string Plain);

    JsonSchema _schema;
    IEnumerable<JsonSchemaProperty> _flattened;
    JsonSchemaProperty _emailProperty;
    JsonSchemaProperty _referenceProperty;

    void Establish() => _schema = _generator.Generate(typeof(AccountRegistered));

    void Because()
    {
        _flattened = _schema.GetFlattenedProperties();
        _emailProperty = _schema.GetSchemaPropertyForPropertyPath(new PropertyPath("Email"));
        _referenceProperty = _schema.GetSchemaPropertyForPropertyPath(new PropertyPath("Reference"));
    }

    [Fact] void should_flatten_all_three_properties() => _flattened.Select(_ => _.Name).ShouldContainOnly("Email", "Reference", "Plain");
    [Fact] void should_resolve_the_string_concept_property() => _emailProperty.ShouldNotBeNull();
    [Fact] void should_treat_the_string_concept_as_a_string_leaf() => _emailProperty.Type.ShouldEqual(JsonObjectType.String);
    [Fact] void should_resolve_the_guid_concept_property() => _referenceProperty.ShouldNotBeNull();
    [Fact] void should_treat_the_guid_concept_as_a_string_leaf_with_guid_format() => _referenceProperty.Format.ShouldEqual("guid");
}
