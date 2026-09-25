// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_CompatibilityValidator;

public class when_generating_client_schema : Specification
{
    string _schema;

    void Because() => _schema = CompatibilityValidator.GenerateClientSchema();

    [Fact] void should_return_a_schema() => _schema.ShouldNotBeNull();
    [Fact] void should_not_be_empty() => _schema.ShouldNotBeEmpty();
    [Fact] void should_use_proto3_syntax() => _schema.ShouldContain("syntax = \"proto3\"");
    [Fact] void should_include_connection_service() => _schema.ShouldContain("ConnectionService");
    [Fact] void should_include_event_sequences_service() => _schema.ShouldContain("EventSequences");
    [Fact] void should_include_all_available_services() =>
        Contracts.AvailableServices.All
            .All(serviceType => _schema.Contains(serviceType.Name.TrimStart('I')))
            .ShouldBeTrue();
}
