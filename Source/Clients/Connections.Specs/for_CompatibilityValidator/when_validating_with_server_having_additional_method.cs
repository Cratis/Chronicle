// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_CompatibilityValidator;

/// <summary>
/// Verifies forward-compatibility: a newer server that exposes additional methods is still
/// compatible with an older client that only knows about a subset of those methods. The
/// compatibility check only requires that the server satisfies every method the client expects.
/// </summary>
public class when_validating_with_server_having_additional_method : Specification
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
          rpc DoSomething (MyRequest) returns (MyResponse);
          rpc DoSomethingNew (MyRequest) returns (MyResponse);
        }
        message MyRequest {}
        message MyResponse {}
        """;

    void Because() => _result = CompatibilityValidator.Validate(ClientSchema, ServerSchema, NullLogger.Instance);

    [Fact] void should_be_compatible() => _result.IsCompatible.ShouldBeTrue();
    [Fact] void should_have_no_errors() => _result.Errors.ShouldBeEmpty();
}
