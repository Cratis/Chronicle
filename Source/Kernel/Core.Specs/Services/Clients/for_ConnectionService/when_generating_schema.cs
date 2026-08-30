// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService;

public class when_generating_schema : Specification
{
    string _schema;

    void Because()
    {
        var generator = new ProtoBuf.Grpc.Reflection.SchemaGenerator
        {
            ProtoSyntax = ProtoBuf.Meta.ProtoSyntax.Proto3
        };
        _schema = generator.GetSchema([typeof(Contracts.Clients.IConnectionService)]);
    }

    [Fact] void should_generate_schema() => _schema.ShouldNotBeNull();
    [Fact] void should_contain_connection_service() => _schema.ShouldContain("ConnectionService");
    [Fact] void should_contain_get_descriptor_set_method() => _schema.ShouldContain("GetDescriptorSet");
}
