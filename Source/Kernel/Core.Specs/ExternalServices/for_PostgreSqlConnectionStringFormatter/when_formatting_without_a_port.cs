// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ExternalServices;

namespace Cratis.Chronicle.ExternalServices.for_PostgreSqlConnectionStringFormatter;

public class when_formatting_without_a_port : Specification
{
    PostgreSqlConnectionStringFormatter _formatter;
    DatabaseEndpointConfiguration _configuration;
    string _result;

    void Establish()
    {
        _formatter = new PostgreSqlConnectionStringFormatter();
        _configuration = new DatabaseEndpointConfiguration(
            "db.example.com",
            DatabasePort.Unspecified,
            "Customers",
            "postgres",
            "secret",
            new Dictionary<string, string>());
    }

    void Because() => _result = _formatter.Format(_configuration);

    [Fact] void should_include_host() => _result.ShouldContain("Host=db.example.com;");
    [Fact] void should_not_include_a_port() => _result.Contains("Port=").ShouldBeFalse();
}
