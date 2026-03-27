// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaExtensions.when_getting_schema_property_for_property_path;

public class with_pascal_case_property_in_camel_case_schema : Specification
{
    record MyModel(string Name, int Count);

    JsonSchema _schema;
    JsonSchemaProperty? _result;

    void Establish() => _schema = JsonSchema.FromType<MyModel>();

    void Because() => _result = _schema.GetSchemaPropertyForPropertyPath(new PropertyPath("Name"));

    [Fact] void should_find_the_property() => _result.ShouldNotBeNull();
    [Fact] void should_return_correct_property() => _result!.Name.ShouldEqual("name");
}
