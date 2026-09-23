// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas.for_SecurityJsonSchemaExtensions.when_checking_for_subject_independent_security_metadata;

public class and_nothing_carries_security_metadata : Specification
{
    const string Json = """
    {
        "type": "object",
        "properties": {
            "name": { "type": "string" }
        }
    }
    """;

    JsonSchema _schema;
    bool _result;

    void Establish() => _schema = JsonSchema.FromJson(Json);

    void Because() => _result = _schema.HasSubjectIndependentSecurityMetadata();

    [Fact] void should_not_recognize_it_as_subject_independent() => _result.ShouldBeFalse();
}
