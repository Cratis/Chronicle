// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Connections.for_CompatibilityValidator;

public class when_validating_identical_schemas : Specification
{
    CompatibilityCheckResult _result;
    string _schema;

    void Establish()
    {
        var generator = new ProtoBuf.Grpc.Reflection.SchemaGenerator
        {
            ProtoSyntax = ProtoBuf.Meta.ProtoSyntax.Proto3
        };
        _schema = generator.GetSchema([typeof(Contracts.Clients.IConnectionService)]);
    }

    void Because() => _result = CompatibilityValidator.Validate(_schema, _schema, NullLogger.Instance);

    [Fact] void should_be_compatible() => _result.IsCompatible.ShouldBeTrue();
    [Fact] void should_have_no_errors() => _result.Errors.ShouldBeEmpty();
}
