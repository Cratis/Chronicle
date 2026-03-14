// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService;

/// <summary>
/// Simulates what <see cref="ConnectionService.GetDescriptorSet"/> returns: a schema built from
/// every service type registered in <see cref="Contracts.AvailableServices"/>, grouped by namespace.
/// </summary>
public class when_getting_descriptor_set : Specification
{
    string _schema;

    void Because()
    {
        var generator = new ProtoBuf.Grpc.Reflection.SchemaGenerator
        {
            ProtoSyntax = ProtoBuf.Meta.ProtoSyntax.Proto3
        };
        var schemas = Contracts.AvailableServices.All
            .GroupBy(t => t.Namespace ?? string.Empty)
            .Select(group => generator.GetSchema(group.ToArray()));
        _schema = string.Join('\n', schemas);
    }

    [Fact] void should_generate_a_schema() => _schema.ShouldNotBeNull();
    [Fact] void should_not_be_empty() => _schema.ShouldNotBeEmpty();
    [Fact] void should_contain_connection_service() => _schema.ShouldContain("ConnectionService");
    [Fact] void should_contain_get_descriptor_set_method() => _schema.ShouldContain("GetDescriptorSet");
    [Fact] void should_include_all_available_services() =>
        Contracts.AvailableServices.All
            .All(serviceType => _schema.Contains(serviceType.Name.TrimStart('I')))
            .ShouldBeTrue();
}
