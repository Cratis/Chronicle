// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator;

public class when_generating_schema_for_object_type : given.a_json_schema_generator
{
    record MyEvent(Guid Id, string Name);

    JsonSchema _result;

    void Because() => _result = _generator.Generate(typeof(MyEvent));

    [Fact] void should_set_title_to_type_name() => _result.Title.ShouldEqual(nameof(MyEvent));
}
