// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Json.for_ExpandoObjectConverter.given;

/// <summary>
/// A schema shaped the way a read model's registered schema actually is - a nullable enum carrying its member
/// list and no format suffix, beside a nullable flag, a non-nullable enum and a non-nullable number.
/// </summary>
/// <remarks>
/// The four properties above are enough to observe the two guards together but not to tell them apart: every one
/// of them is suppressed by nullability <em>and</em> by its member list, so either guard alone accounts for the
/// whole result. <c>feedback</c> and <c>decision</c> are the two subjects that separate them - a nullable enum
/// whose zero <em>is</em> declared, which only the nullability guard can suppress, and its non-nullable twin,
/// which nothing may suppress because a type default that is a declared member is a legal value and must still
/// be written.
/// <para>
/// The shapes are the generator's, not invented for the fixture: a real <c>enum? Reason</c> renders as
/// <c>{"type":["integer","null"],"enum":[...],"x-enumNames":[...]}</c> and its non-nullable counterpart as the
/// same without <c>"null"</c> - see <c>for_JsonSchemaGenerator.when_generating_schema_for_a_type_with_an_enum</c>,
/// which pins that wire.
/// </para>
/// </remarks>
public class an_expando_object_converter_with_a_read_model_schema : Specification
{
    protected ExpandoObjectConverter converter;
    protected JsonSchema schema;

    void Establish()
    {
        schema = JsonSchema.FromJson(
            """
            {
                "type": "object",
                "properties": {
                    "id": { "type": "string" },
                    "rejectionReason": {
                        "type": ["integer", "null"],
                        "enum": [1, 2, 3, 4],
                        "x-enumNames": ["RejectedBySigner", "Canceled", "Expired", "Failed"]
                    },
                    "status": {
                        "type": "integer",
                        "enum": [1, 2, 3],
                        "x-enumNames": ["Draft", "Signed", "Terminated"]
                    },
                    "isFinalized": { "type": ["boolean", "null"] },
                    "feedback": {
                        "type": ["integer", "null"],
                        "enum": [0, 1, 2],
                        "x-enumNames": ["NotSet", "Positive", "Negative"]
                    },
                    "decision": {
                        "type": "integer",
                        "enum": [0, 1, 2],
                        "x-enumNames": ["NotSet", "Approved", "Rejected"]
                    },
                    "rate": { "type": "number", "format": "decimal" }
                }
            }
            """);
        converter = new(new TypeFormats());
    }
}
