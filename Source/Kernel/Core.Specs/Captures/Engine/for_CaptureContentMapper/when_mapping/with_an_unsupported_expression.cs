// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.Engine.for_CaptureContentMapper.when_mapping;

public class with_an_unsupported_expression : Specification
{
    CaptureContentMapper _mapper;
    Exception _error;

    void Establish() => _mapper = new();

    void Because() => _error = Catch.Exception(() => _mapper.Map(
        new AppendDefinition(
            "CustomerChanged",
            new WhenClause(WhenClauseType.Added, []),
            new Dictionary<string, string> { ["tenant"] = "$context.tenantId" }),
        new CaptureChange("42", CaptureChangeType.Added, null, new JsonObject())));

    [Fact] void should_throw_unsupported_capture_capability() => _error.ShouldBeOfExactType<UnsupportedCaptureCapability>();
}
