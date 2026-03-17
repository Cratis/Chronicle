// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_CompatibilityValidator;

/// <summary>
/// Simulates the real-world connection scenario: the client generates its schema from local
/// contracts and the server generates its schema from the same contracts assembly. Both schemas
/// must be compatible so that the compatibility check in <see cref="ChronicleConnection"/> succeeds.
/// </summary>
public class when_validating_client_schema_against_server_schema : Specification
{
    CompatibilityCheckResult _result;
    string _clientSchema;
    string _serverSchema;

    void Establish()
    {
        _clientSchema = CompatibilityValidator.GenerateClientSchema();

        // Reproduce the server-side schema generation from ConnectionService.GenerateSchema().
        // Both sides derive their schemas from the same Contracts assembly, so they must match.
        var generator = new ProtoBuf.Grpc.Reflection.SchemaGenerator
        {
            ProtoSyntax = ProtoBuf.Meta.ProtoSyntax.Proto3
        };
        var schemas = Contracts.AvailableServices.All
            .GroupBy(t => t.Namespace ?? string.Empty)
            .Select(group => generator.GetSchema(group.ToArray()));
        _serverSchema = string.Join('\n', schemas);
    }

    void Because() => _result = CompatibilityValidator.Validate(_clientSchema, _serverSchema, NullLogger.Instance);

    [Fact] void should_be_compatible() => _result.IsCompatible.ShouldBeTrue();
    [Fact] void should_have_no_errors() => _result.Errors.ShouldBeEmpty();
}
