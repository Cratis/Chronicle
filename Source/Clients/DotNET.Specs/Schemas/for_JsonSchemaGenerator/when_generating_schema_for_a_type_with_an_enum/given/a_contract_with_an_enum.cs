// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaGenerator.when_generating_schema_for_a_type_with_an_enum.given;

/// <summary>
/// A read model with an optional and a required enum of the same 1-based type - the shape the withheld-default
/// behavior is decided on - plus a nullable enum that declares a zero member.
/// </summary>
/// <remarks>
/// Two independent readings decide whether a default is withheld, and on a 1-based enum both of them say withhold
/// it, so any assertion about that type alone is satisfied by either reading on its own. The enum that declares a
/// zero is the one subject only nullability can account for, which is what makes an assertion about it able to
/// fail for one specific reason.
/// </remarks>
public class a_contract_with_an_enum : for_JsonSchemaGenerator.given.a_json_schema_generator
{
    protected enum RejectionReason
    {
        RejectedBySigner = 1,
        Expired = 2,
        Withdrawn = 3
    }

    protected enum Sentiment
    {
        NotSet = 0,
        Positive = 1,
        Negative = 2
    }

    protected JsonSchema _result;

    protected JsonSchemaProperty PropertyNamed(string name) => _result.GetFlattenedProperties().Single(_ => _.Name == name);

    protected long[] MembersOf(string name) => PropertyNamed(name).Enumeration.Select(_ => Convert.ToInt64(_, CultureInfo.InvariantCulture)).ToArray();

    protected sealed record Contract(Guid Id, RejectionReason? Reason, RejectionReason Status, Sentiment? Feedback);
}
