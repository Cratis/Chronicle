// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_CompatibilityValidator;

public class when_validating_with_incompatible_method_signature : Specification
{
    CompatibilityCheckResult _result;

    const string ClientSchema = """
        syntax = "proto3";
        service MyService {
          rpc DoSomething (MyRequest) returns (MyResponse);
        }
        message MyRequest {}
        message MyResponse {}
        """;

    const string ServerSchema = """
        syntax = "proto3";
        service MyService {
          rpc DoSomething (MyRequest) returns (stream MyResponse);
        }
        message MyRequest {}
        message MyResponse {}
        """;

    void Because() => _result = CompatibilityValidator.Validate(ClientSchema, ServerSchema, NullLogger.Instance);

    [Fact] void should_not_be_compatible() => _result.IsCompatible.ShouldBeFalse();
    [Fact] void should_have_error_about_incompatible_signature() => _result.Errors.ShouldContain(e => e.Contains("DoSomething") && e.Contains("incompatible"));
}
